using System;
using System.Text;
using Azure.Messaging.ServiceBus;
using FunctionAppsDemo.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FunctionAppsDemo.Functions.Model;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System.IO;
using Azure.Storage.Blobs.Specialized;
using Azure;

namespace FunctionAppsDemo.Functions
{
    /// <summary>
    /// Represents a class responsible for processing messages related to three errors received from a Service Bus queue.
    /// </summary>
    public class ThreeErrors
    {
        /// <summary>
        /// Handles the processing of messages related to three errors in uner a minute received from a Service Bus queue.
        /// </summary>
        /// <param name="message">The received Service Bus message containing the three errors data.</param>
        /// <param name="messageActions">The actions to perform on the Service Bus message.</param>
        /// <param name="log">The logger instance for logging information.</param>
        /// <param name="context">The execution context for the function.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [FunctionName("threeErrors")]
        public async Task RunAsync(
            [ServiceBusTrigger("%QueueNameThreeErrors%", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions, ILogger log, ExecutionContext context)
        {
            var myQueueItem = Encoding.UTF8.GetString(message.Body);

            log.LogInformation(
                $"C# ServiceBus queue trigger. Invocation ID: {context.InvocationId} Function processed message: {myQueueItem}");

            var data = JsonConvert.DeserializeObject<ThreeErrorsMessage>(myQueueItem);
            if (data == null || string.IsNullOrEmpty(data.ConnectionDeviceId))
            {
                log.LogInformation("data was null or data.ConnectionDeviceId was null or empty");
                await messageActions.DeadLetterMessageAsync(message);
            }
            else
            {
                log.LogInformation($"C# ServiceBus queue trigger function processed message: {message}");
                string serviceConnectionString = System.Environment.GetEnvironmentVariable("IoTHubConnectionString");
                using var serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString);
                using var registryManager = RegistryManager.CreateFromConnectionString(serviceConnectionString);

                var manager = new IoTHubManager(serviceClient, registryManager);

                log.LogInformation("Saving data into blob container...");
                await Handler.HandleBlobs(System.Environment.GetEnvironmentVariable("Storage"), 
                    message.Body,
                    log,
                    $"{data.ConnectionDeviceId}_{DateTime.UtcNow}",
                    System.Environment.GetEnvironmentVariable("ThreeErrorsBlobContainerName"));
                log.LogInformation("Firing 'EmergencyStop' method");
                await manager.ExecuteDeviceMethod("EmergencyStop", data.ConnectionDeviceId);
                await messageActions.CompleteMessageAsync(message);
            }
        }
    }
}