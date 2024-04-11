using System.Net.Mime;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpcAgent.Lib.Enums;

namespace OpcAgent.Lib.Device
{
    public class VirtualDevice(DeviceClient deviceClient)

    {
        private double _defaultSendFrequency = 60 * 5.0; //5 minutes
        public DeviceClient DeviceClient => deviceClient;
        public event Action? EmergencyStopRequested;
        public event Action? ResetErrorStatusRequested;

        public event Action<int>? DesiredProductionRateChanged;

        public event Action<double>? DesiredSendFrequencyChanged;

        #region Sending Messages

        public async Task SendMessage(Message message)
        {
            Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message");
            await deviceClient.SendEventAsync(message);
            message.Dispose();
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

        private static async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");

            await Task.Delay(1000);

            return new MethodResponse(0);
        }

        #endregion Direct Methods

        #region Device Twin

        public async Task UpdateProductionRateAsync(int productionRate)
        {
            var reportedProperties = new TwinCollection
            {
                ["ProductionRate"] = productionRate
            };
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        public async Task UpdateErrorsAsync(int errors)
        {
            var reportedProperties = new TwinCollection
            {
                ["DeviceErrors"] = errors
            };
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        private Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            if (desiredProperties.Contains("ProductionRate"))
            {
                int desiredProductionRate = desiredProperties["ProductionRate"];
                DesiredProductionRateChanged?.Invoke(desiredProductionRate);
            }

            if (desiredProperties.Contains("telemetryConfig"))
            {
                var telemetryConfig = desiredProperties["telemetryConfig"] as JObject;
                if (telemetryConfig != null && telemetryConfig.TryGetValue("sendFrequency", out var sendFrequency))
                {
                    DesiredSendFrequencyChanged?.Invoke(
                        DeviceTwinPropertyParser.ConvertSendFrequencyToSeconds(sendFrequency.ToString()));
                }
            }

            return Task.CompletedTask;
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

        private Task<MethodResponse> ResetErrorStatusHandler(MethodRequest methodRequest, object usercontext)
        {
            ResetErrorStatusRequested?.Invoke();
            return Task.FromResult(new MethodResponse(0));
        }

        private Task<MethodResponse> EmergencyStopHandler(MethodRequest methodRequest, object _)
        {
            EmergencyStopRequested?.Invoke();
            return Task.FromResult(new MethodResponse(0));
        }

        public async Task<double> GetSendFrequency()
        {
            var twin = await this.DeviceClient.GetTwinAsync();
            var desiredProperties = twin.Properties.Desired;

            if (!desiredProperties.Contains("telemetryConfig")) return _defaultSendFrequency;

            var telemetryConfig = desiredProperties["telemetryConfig"] as JObject;

            if (telemetryConfig == null || !telemetryConfig.TryGetValue("sendFrequency", out var sendFrequency))
                return _defaultSendFrequency;
            try
            {
                return DeviceTwinPropertyParser.ConvertSendFrequencyToSeconds(sendFrequency.ToString());
            }
            catch (ArgumentException argumentException)
            {
                Console.WriteLine(argumentException.ToString());
            }

            return _defaultSendFrequency;
        }
    }
}