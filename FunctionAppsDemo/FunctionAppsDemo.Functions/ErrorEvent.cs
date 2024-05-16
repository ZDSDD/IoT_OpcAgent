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
using System.Collections.Generic;
using Azure.Communication.Email;

namespace FunctionAppsDemo.Functions;

public class ErrorEvent
{
    [FunctionName("ErrorEvent")]
    public async Task Run(
        [ServiceBusTrigger("%QueueNameErrorEvent%", Connection = "ServiceBusConnectionString")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, ILogger log, ExecutionContext context)
    {
        var myQueueItem = Encoding.UTF8.GetString(message.Body);

        log.LogInformation(
            $"C# ServiceBus queue trigger. Invocation ID: {context.InvocationId} Function processed message: {myQueueItem}");

        var data = JsonConvert.DeserializeObject<ErrorMessage>(myQueueItem);
        if (data == null || ReferenceEquals(null, data.errors))
        {
            await messageActions.DeadLetterMessageAsync(message);
        }
        else
        {
            if (data.increased == "true")
            {
                List<string> recipientEmails = new List<string>
                {
                    // "Recipient1@example.com",
                    // "Recipient2@example.com",
                };
                foreach (String email in recipientEmails)
                {
                    await Handler.SendEmail(log, email, $"There was an error with{data.deviceNodeId}\n" +
                                                        $"Device error code: {data.errors}");
                }
            }
            else
            {
                log.LogInformation("Error event indicates either a decrease in errors or the absence of errors.");
            }

            await messageActions.CompleteMessageAsync(message);
        }
    }
}