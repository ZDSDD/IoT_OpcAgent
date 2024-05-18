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

/// <summary>
/// Represents a class responsible for handling error events received from a Service Bus queue.
/// </summary>
public class ErrorEvent
{
    /// <summary>
    /// Handles the processing of error events received from a Service Bus queue.
    /// </summary>
    /// <param name="message">The received Service Bus message containing the error event data.</param>
    /// <param name="messageActions">The actions to perform on the Service Bus message.</param>
    /// <param name="log">The logger instance for logging information.</param>
    /// <param name="context">The execution context for the function.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
        if (data == null || ReferenceEquals(null, data.Errors))
        {
            await messageActions.DeadLetterMessageAsync(message);
            return;
        }

        if (data.ErrorsIncreased == "true")
        {
                await Handler.SendEmail(log, System.Environment.GetEnvironmentVariable("emailTo"), $"There was an error with: {data.DeviceNode}\n" +
                                                    $"Device error code: {data.Errors}");
        }
        else
        {
            log.LogInformation("Error event indicates either a decrease in errors or the absence of errors.");
        }

        await messageActions.CompleteMessageAsync(message);
    }
}