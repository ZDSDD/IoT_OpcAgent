using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using OpcAgent.Lib.Device;

namespace OpcAgent.Lib.Managers;

public class ProductionLineManager(ServiceBusSender serviceBusSender)
{
    private List<VirtualDevice> _devices = [];

    public void AddDevice(VirtualDevice virtualDevice)
    {
        _devices.Add(virtualDevice);
        virtualDevice.OnErrorsChange += OnErrorsChange;
    }

    private void OnErrorsChange(string deviceid, int deviceErrors, bool increase)
    {
        var json = JsonConvert.SerializeObject(new { deviceId = deviceid, errors = deviceErrors, increased = increase ? "true" : "false"});
        var message = new ServiceBusMessage(json);
        serviceBusSender.SendMessageAsync(message);
    }
}