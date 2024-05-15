using System;
using Azure.Messaging.ServiceBus;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FunctionAppsDemo.Functions.Model;
using Newtonsoft.Json;
using System.Text;

namespace FunctionAppsDemo.Functions
{
    public class ProductionRate
    {
        [FunctionName("ProductionRate")]
        public async Task RunAsync(
            [ServiceBusTrigger("%QueueNameProduction%", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions, ILogger log, ExecutionContext context)
        {
            var myQueueItem = Encoding.UTF8.GetString(message.Body);

            log.LogInformation(
                $"C# ServiceBus queue trigger. Invocation ID: {context.InvocationId} Function processed message: {myQueueItem}");

            ProductionMessage data = null;
            try
            {
                data = JsonConvert.DeserializeObject<ProductionMessage>(myQueueItem);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
            if (data == null || string.IsNullOrEmpty(data.DeviceId))
            {
                log.LogInformation("data was null or data.DeviceId was null or empty");
                await messageActions.DeadLetterMessageAsync(message);
                return;
            }

            await Handler.HandleDesiredProductionRate(data, System.Environment.GetEnvironmentVariable("IoTHubConnectionString"), log);

            await Handler.HandleBlobs(
                System.Environment.GetEnvironmentVariable("Storage"),
                message.Body,
                log,
                $"{data.DeviceId}_{data.WindowStartTime}",
                System.Environment.GetEnvironmentVariable("productionBlobContainerName"));
            await messageActions.CompleteMessageAsync(message);
        }


    }
}