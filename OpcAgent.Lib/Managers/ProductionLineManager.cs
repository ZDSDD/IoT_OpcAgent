using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using OpcAgent.Lib.Device;

namespace OpcAgent.Lib.Managers;

public class ProductionLineManager()
{
    private readonly List<VirtualDevice> _devices = [];

    public void AddDevice(VirtualDevice virtualDevice)
    {
        _devices.Add(virtualDevice);
    }

    public void PrintDevices()
    {
        for (int i = 0; i < _devices.Count; i++)
        {
            Console.WriteLine($"{i} > {_devices[i].NodeId}");
        }
    }

    public bool AlreadyExists(string deviceNodeId)
    {
        return _devices.Exists(x => x.NodeId.ToString() == deviceNodeId);
    }
}