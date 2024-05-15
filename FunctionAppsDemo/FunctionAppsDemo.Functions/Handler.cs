﻿using Azure.Storage.Blobs;
using Azure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FunctionAppsDemo.Functions.Model;
using Microsoft.Azure.Devices;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FunctionAppsDemo.Functions
{
    internal static class Handler
    {
        public static async Task HandleBlobs(string blobServiceConnectionString, 
            BinaryData message,
            ILogger log,
            String  blobName,
            String blobContainerName)
        {

            BlobServiceClient blobServiceClient = new BlobServiceClient(blobServiceConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);

            // Check if the blob already exists

            var blob = containerClient.GetBlobClient(blobName);
            bool blobExists = await blob.ExistsAsync();
            if (blobExists)
            {
                await AppendBlob(message, blob);
            }
            else
            {
                try
                {
                    // Try to upload the blob

                    await containerClient.UploadBlobAsync(blobName, message);
                }
                catch (RequestFailedException ex) when (ex.ErrorCode == "BlobAlreadyExists")
                {
                    // If the blob already exists, handle the situation gracefully
                    // For example, you can retry with a different blob name or log a message
                    log.LogWarning($"Blob '{blobName}' already exists. Retrying with appending the blob.");
                    await AppendBlob(message, blob);
                }
                catch (Exception ex)
                {
                    log.LogWarning("Was some other exception" + ex.Message);
                }
            }

        }

        private static async Task AppendBlob(BinaryData message, BlobClient blob)
        {
            // If the blob already exists, download its content

            var downloadResponse = await blob.DownloadAsync();
            using var streamReader = new StreamReader(downloadResponse.Value.Content);
            // Read the existing content
            var existingContent = await streamReader.ReadToEndAsync();

            // Append the new message to the existing content
            existingContent += $"\n{message}";

            // Upload the updated content back to the blob
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(existingContent));
            await blob.UploadAsync(stream, true);
        }
        
        public static async Task HandleDesiredProductionRate(ProductionMessage data, string serviceConnectionString, ILogger log)
        {
            // Setup connection to Azure services
            using var serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString);
            using var registryManager = RegistryManager.CreateFromConnectionString(serviceConnectionString);
            var manager = new IoTHubManager(serviceClient, registryManager);

            int currentDesiredProductionRate = await manager.GetDesiredTwinValue(data.DeviceId, "ProductionRate");
            if (currentDesiredProductionRate == 0)
            {
                log.LogInformation($"{data.DeviceId}\tcurrent Desired Production Rate is uqual to 0. There is no need to furher decrease it.");
                return;
            }
            if(data.TotalGoodCount == 0 && data.TotalBadCount == 0){
                log.LogInformation($"{data.DeviceId}\tGoodCount and BadCount was both equal 0. Likely the device hasn't started yet");
                return;
            }
            // Calculate the ratio between total [good producs / total products]
            float denominator = data.TotalGoodCount + data.TotalBadCount;

            //Check to not divide by 0
            if (denominator == 0) denominator = 1;

            float ratio = (data.TotalGoodCount / denominator) * 100.0f;
            bool shouldDecreaseProductionRate = ratio < 90f;

            if (!shouldDecreaseProductionRate ){

                log.LogInformation($"{data.DeviceId} ratio was: {ratio}.\tNo need to decrease desired production rate");
                return;
            }
            else
            {
                log.LogInformation($"{data.DeviceId} ratio was: {ratio}.\tProceeding to decrease desired production rate");
            }

            //Finaly, update the desired twin. Don't go below 0!
            await manager.UpdateDesiredTwin(
                data.DeviceId,
                "ProductionRate",
                currentDesiredProductionRate <= 10
                    ? 0
                    : currentDesiredProductionRate - 10);
        }
    }
}
