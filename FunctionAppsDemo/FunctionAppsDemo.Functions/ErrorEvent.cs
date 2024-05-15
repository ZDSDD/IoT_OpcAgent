using Azure.Messaging.ServiceBus;
using FunctionAppsDemo.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace FunctionAppsDemo.Functions
{
    public class ErrorEvent
    {
        [FunctionName("ErrorEvent")]
        public async Task Run([ServiceBusTrigger("%QueueNameErrorEvent%", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions, ILogger log, ExecutionContext context)
        {
            var myQueueItem = Encoding.UTF8.GetString(message.Body);
            string queueName = context.FunctionName;

            log.LogInformation($"C# ServiceBus queue trigger. Invocation ID: {context.InvocationId} Function processed message: {myQueueItem}");

            var data = JsonConvert.DeserializeObject<ErrorMessage>(myQueueItem);
            if (data == null || ReferenceEquals(null, data.errors))
            {
                await messageActions.DeadLetterMessageAsync(message);
            }
            else
            {
                if (data.increased == "true")
                {
                    // todo: send emails
                    Console.WriteLine(data.errors);
                }
                else
                {
                    log.LogInformation("Error event indicates either a decrease in errors or the absence of errors.");
                }
                await messageActions.CompleteMessageAsync(message);
            }
        }

    }
}
