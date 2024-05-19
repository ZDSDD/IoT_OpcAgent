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
        Console.WriteLine();
        Console.WriteLine(@"Available devices: ");
        for (int i = 1; i <= _devices.Count; i++)
            Console.WriteLine($@"{i} > {_devices[i - 1].NodeId}");
        Console.WriteLine();
    }

    public bool HasItems()
    {
        return _devices.Count != 0;
    }

    public bool AlreadyExists(string deviceNodeId)
    {
        return _devices.Exists(x => x.NodeId.ToString() == deviceNodeId);
    }

    public void Delete(int input)
    {
        if (input < 0 || input >= _devices.Count)
        {
            Console.WriteLine(@"wrong index");
            return;
        }
        var deviceToDelete = _devices[input];
        deviceToDelete.Dispose();
        try
        {
            _devices.Remove(deviceToDelete);
        }
        catch (ArgumentOutOfRangeException e)
        {
            Console.WriteLine("Couldn't remove device. "+ e.Message);
        }

        Console.WriteLine(@"removed successfully");
    }
}