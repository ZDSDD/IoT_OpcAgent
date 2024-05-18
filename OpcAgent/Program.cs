using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using OpcAgent.Lib;
using OpcAgent.Lib.Device;
using OpcAgent.Lib.Managers;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

//Get keys from secrets.json
IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

//Initialize OPCClient
string serverAddress = config.GetConnectionString("serverAddress");
using var opcClient = new OpcClient(serverAddress);
try
{
    opcClient.Connect();
}
catch (InvalidOperationException exception)
{
    Console.WriteLine(exception.Message);
    Console.WriteLine($@"Your server address: {serverAddress}");
    return;
}
catch (OpcException exception)
{
    Console.WriteLine(exception.Message);
    Console.WriteLine(@"Closing agent. Check if server runs properly and try again.");
    return;
}
// Get all devices defined in secrets.json
IConfigurationSection devicesSection = config.GetSection("Devices");

ProductionLineManager productionLineManager = new ProductionLineManager();
foreach (IConfigurationSection device in devicesSection.GetChildren())
{
    // Get device details
    string deviceConnectionString = device["DeviceConnectionString"];
    string deviceNodeId = device["DeviceNodeId"];
    Console.WriteLine(deviceConnectionString);

    //Initialize Device Client

    var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
    var nodeId = new NodeId(deviceNodeId);
    var opcRepository = new OpcRepository(opcClient, OpcUtils.InitReadNodes(nodeId));
    //Initialize Virtual Device
    if (opcRepository.IsOk())
    {
        productionLineManager.AddDevice(new VirtualDevice(nodeId, opcClient, deviceClient, opcRepository));
    }
    else
    {
        Console.WriteLine("\nThere is currently error with " + nodeId);
        Console.WriteLine("Checking method pass...");
        int passed = opcRepository.MethodsPassedCount();
        Console.WriteLine($"\nMethod passed: {passed}. Total methods = {opcRepository.TotalMethods}");
        if (passed == 0)
        {
            Console.WriteLine("Check if device is running or if device node id is properly set.");
        }
    }

    await Task.Delay(100);
}

Console.ReadLine();