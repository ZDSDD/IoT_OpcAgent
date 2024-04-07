﻿using System.Net.Mime;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace OpcAgent.Lib.Device
{
    public class VirtualDevice(DeviceClient deviceClient)
    
    {
        #region Sending Messages

        public async Task SendMessages(int nrOfMessages, int delay)
        {
            var rnd = new Random();

            Console.WriteLine($"Device sending {nrOfMessages} messages to IoTHub...\n");

            for (int count = 0; count < nrOfMessages; count++)
            {
                var data = new
                {
                    temperature = rnd.Next(20, 35),
                    humidity = rnd.Next(60, 80),
                    msgCount = count
                };

                var dataString = JsonConvert.SerializeObject(data);

                Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
                eventMessage.ContentType = MediaTypeNames.Application.Json;
                eventMessage.ContentEncoding = "utf-8";
                eventMessage.Properties.Add("temperatureAlert", (data.temperature > 30) ? "true" : "false");
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message: {count}, Data: [{dataString}]");

                await deviceClient.SendEventAsync(eventMessage);

                if (count < nrOfMessages - 1)
                    await Task.Delay(delay);
            }
            Console.WriteLine();
        }

        #endregion Sending Messages

        #region Receiving Messages

        private async Task OnC2dMessageReceivedAsync(Message receivedMessage, object _)
        {
            Console.WriteLine($"\t{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
            PrintMessage(receivedMessage);

            await deviceClient.CompleteAsync(receivedMessage);
            Console.WriteLine($"\t{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");

            receivedMessage.Dispose();
        }

        private void PrintMessage(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            Console.WriteLine($"\t\tReceived message: {messageData}");

            int propCount = 0;
            foreach (var prop in receivedMessage.Properties)
            {
                Console.WriteLine($"\t\tProperty[{propCount++}> Key={prop.Key} : Value={prop.Value}");
            }
        }

        #endregion Receiving Messages

        #region Direct Methods

        private async Task<MethodResponse> SendMessagesHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");

            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { nrOfMessages = default(int), delay = default(int) });

            await SendMessages(payload.nrOfMessages, payload.delay);

            return new MethodResponse(0);
        }

        private static async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");

            await Task.Delay(1000);

            return new MethodResponse(0);
        }

        #endregion Direct Methods

        #region Device Twin

        public async Task UpdateTwinAsync()
        {
            var twin = await deviceClient.GetTwinAsync();

            Console.WriteLine($"\nInitial twin value received: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");
            Console.WriteLine();

            var reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            Console.WriteLine($"\tDesired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
            Console.WriteLine("\tSending current time as reported property");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
        }

        #endregion Device Twin

        public async Task InitializeHandlers()
        {
            await deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, deviceClient);

            await deviceClient.SetMethodHandlerAsync("SendMessages", SendMessagesHandler, deviceClient);
            await deviceClient.SetMethodDefaultHandlerAsync(DefaultServiceHandler, deviceClient);

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, deviceClient);
        }
    }
}
