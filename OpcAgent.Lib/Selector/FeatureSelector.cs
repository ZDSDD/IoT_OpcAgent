using Microsoft.Azure.Devices.Client;
using Opc.Ua;
using Opc.UaFx.Client;
using OpcAgent.Lib.Device;
using OpcAgent.Lib.Managers;

namespace OpcAgent.Lib.Selector;

public static class FeatureSelector
{
    public static void PrintMenu(ProductionLineManager manager)
    {
        Console.WriteLine(@"

1 - Add new device");
        if (manager.HasItems()) Console.WriteLine(@"2 - Delete device");
        Console.WriteLine(@"0 - exit");
    }


    public static int ReadInput()
    {
        var keyPressed = Console.ReadKey();
        var isParsed = int.TryParse(keyPressed.KeyChar.ToString(), out int result);
        return isParsed ? result : -1;
    }

    public static async Task Execute(int feature, ProductionLineManager productionLineManager, OpcClient opcClient)
    {
        switch (feature)
        {
            case 1:
            {
                string deviceNodeId;
                do
                {
                    System.Console.WriteLine(@"
Enter device_node_id (confirm with enter):");
                    deviceNodeId = System.Console.ReadLine() ?? string.Empty;
                } while (productionLineManager.AlreadyExists(deviceNodeId));

                if (deviceNodeId == string.Empty)
                {
                    Console.WriteLine(@"You entered empty value");
                    break;
                }

                System.Console.WriteLine(@"Type device connection string (confirm with enter):");
                string deviceConnectionString = System.Console.ReadLine() ?? string.Empty;
                if (deviceConnectionString == string.Empty)
                {
                    Console.WriteLine(@"You entered empty value");
                    break;
                }

                await AddDevice(deviceNodeId, opcClient, deviceConnectionString, productionLineManager);

                break;
            }
            case 2:
            {
                if (!productionLineManager.HasItems())
                {
                    Console.WriteLine(@"There are no devices to delete");
                    break;
                }

                productionLineManager.PrintDevices();
                Console.WriteLine(@"Which device you want to delete? Pick number and press Enter: ");
                string input = Console.ReadLine() ?? string.Empty;

                if (int.TryParse(input, out int number))
                {
                    Console.WriteLine($@"You entered: {number}");
                }
                else
                {
                    Console.WriteLine(@"That is not a valid integer.");
                }

                Console.WriteLine("You picked device number " + input);
                productionLineManager.Delete(number - 1);
                break;
            }
            default:
                break;
        }
    }

    public static async Task AddDevice(string deviceNodeId, OpcClient opcClient, string deviceConnectionString,
        ProductionLineManager productionLineManager)
    {
        var nodeId = new NodeId(deviceNodeId);
        var opcRepository = new OpcRepository(opcClient, OpcUtils.InitReadNodes(nodeId));
        bool deviceIsConnected = opcRepository.CheckConnection();

        while (opcClient.State is OpcClientState.Reconnecting or OpcClientState.Connecting)
        {
            Console.WriteLine(@"waiting for opcClient to connect ...");
            await Task.Delay(500);
        }

        if (!deviceIsConnected)
        {
            Console.WriteLine(
                $@"Couldn't add the device {deviceNodeId}. Check if device is connected to the OPC UA server.");
            return;
        }

        //Initialize Virtual Device
        if (opcClient.State == OpcClientState.Connected)
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
            productionLineManager.AddDevice(new VirtualDevice(nodeId, opcClient, deviceClient, opcRepository));
            Console.WriteLine(@"successfully added device with nodeId : " + nodeId);
        }
    }
}