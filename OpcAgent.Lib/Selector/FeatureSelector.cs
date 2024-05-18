using Microsoft.Azure.Devices.Client;
using Opc.Ua;
using Opc.UaFx.Client;
using OpcAgent.Lib.Device;
using OpcAgent.Lib.Managers;

namespace OpcAgent.Lib.Selector;

public static class FeatureSelector
{
    public static void PrintMenu()
    {
        System.Console.WriteLine("""
                                 
                                     1 - Add new device
                                     2 - Delete device
                                     0 - Exit
                                 """);
    }


    public static int ReadInput()
    {
        var keyPressed = Console.ReadKey();
        var isParsed = int.TryParse(keyPressed.KeyChar.ToString(), out int result);
        return isParsed ? result : -1;
    }

    public static Task Execute(int feature, ProductionLineManager manager, OpcClient opcClient)
    {
        switch (feature)
        {
            case 1:
            {
                opcClient.Connect();
                string deviceNodeId;
                do
                {
                    System.Console.WriteLine("\nEnter device_node_id (confirm with enter):");
                    deviceNodeId = System.Console.ReadLine() ?? string.Empty;
                } while (manager.AlreadyExists(deviceNodeId));
                if (deviceNodeId == string.Empty)
                {
                    Console.WriteLine("You entered empty value");
                    break;
                }
                System.Console.WriteLine("Type device connection string (confirm with enter):");
                string deviceConnectionString = System.Console.ReadLine() ?? string.Empty;
                if (deviceConnectionString == string.Empty)
                {
                    Console.WriteLine("You entered empty value");
                    break;
                }
                var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
                var nodeId = new NodeId(deviceNodeId);
                var opcRepository = new OpcRepository(opcClient, OpcUtils.InitReadNodes(nodeId));
                //Initialize Virtual Device
                if (opcRepository.IsOk())
                {
                    manager.AddDevice(new VirtualDevice(nodeId, opcClient, deviceClient, opcRepository));
                    System.Console.WriteLine("Device added successfully");
                }
                else
                {
                    Console.WriteLine("There was an error adding the device");
                }

                break;
            }
            case 2:
            {
                manager.PrintDevices();
                break;
            }
            default:
                break;
        }

        return Task.CompletedTask;
    }
}