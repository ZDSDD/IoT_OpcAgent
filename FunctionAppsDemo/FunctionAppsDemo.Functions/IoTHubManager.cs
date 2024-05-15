using Microsoft.Azure.Devices;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionAppsDemo.Functions
{
    public class IoTHubManager
    {

        private ServiceClient client;
        private RegistryManager registry;

        public IoTHubManager(ServiceClient client, RegistryManager registry)
        {
            this.client = client;
            this.registry = registry;
        }

        public async Task SendMessage(string messageText, string deviceId)
        {
            var messageBody = new { text = messageText };
            var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageBody)));
            message.MessageId = Guid.NewGuid().ToString();
            await client.SendAsync(deviceId, message);
        }

        public async Task<int> ExecuteDeviceMethod(string methodName, string deviceId)
        {
            var method = new CloudToDeviceMethod(methodName);

            var result = await client.InvokeDeviceMethodAsync(deviceId, method);
            return result.Status;
        }

        public async Task UpdateDesiredTwin(string deviceId, string propertyName, dynamic propertyValue)
        {
            var twin = await registry.GetTwinAsync(deviceId);
            twin.Properties.Desired[propertyName] = propertyValue;
            try
            {
                await registry.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);
            }
            catch(Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
        }

        public async Task<dynamic> GetDesiredTwinValue(string deviceId, string propertyName)
        {
            var twin = await registry.GetTwinAsync(deviceId);
            return twin.Properties.Desired[propertyName];
        }
    }
}
