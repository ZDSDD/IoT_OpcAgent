using System.Net.Mime;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace OpcAgent.Lib.Device
{
    public class VirtualDevice(DeviceClient deviceClient)

    {
        public DeviceClient DeviceClient => deviceClient;
        public event Action EmergencyStopRequested;
        public event Action ResetErrorStatusRequested;

        #region Sending Messages

        public async Task SendMessages(PayloadData data)
        {
                var dataString = JsonConvert.SerializeObject(data);
                Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
                eventMessage.ContentType = MediaTypeNames.Application.Json;
                eventMessage.ContentEncoding = "utf-8";
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message:\n Data: [{dataString}]");
                await deviceClient.SendEventAsync(eventMessage);
                eventMessage.Dispose();
            Console.WriteLine();
        }

        #endregion Sending Messages

        #region Receiving Messages

        private async Task OnC2dMessageReceivedAsync(Message receivedMessage, object _)
        {
            Console.WriteLine(
                $"\t{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
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
        public struct PayloadData
        {
            public int ProductionStatus { get; set; }
            public string WorkorderId { get; set; }
            public int GoodCount { get; set; }
            public int BadCount { get; set; }
            public float Temperature { get; set; }
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

            Console.WriteLine(
                $"\nInitial twin value received: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");
            Console.WriteLine();

            var reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            Console.WriteLine($"\tDesired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
            Console.WriteLine("\tSending current time as reported property");
            TwinCollection reportedProperties = new TwinCollection
                {
                    ["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now
                };

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
        }

        #endregion Device Twin

        public async Task InitializeHandlers()
        {
            await deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, deviceClient);

            await deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStopHandler, deviceClient);
            
            await deviceClient.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatusHandler, deviceClient);
            await deviceClient.SetMethodDefaultHandlerAsync(DefaultServiceHandler, deviceClient);

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, deviceClient);
        }

        private Task<MethodResponse> ResetErrorStatusHandler(MethodRequest methodrequest, object usercontext)
        {
            ResetErrorStatusRequested?.Invoke();
            return Task.FromResult(new MethodResponse(0));
        }

        private Task<MethodResponse> EmergencyStopHandler(MethodRequest methodRequest, object _)
        {
            EmergencyStopRequested?.Invoke();
            return Task.FromResult(new MethodResponse(0));
        }
    }
}