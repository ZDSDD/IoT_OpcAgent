using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Opc.Ua;
using Opc.UaFx.Client;
using OpcAgent.Lib.Device;
using OpcAgent.Lib.Managers;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

//Get keys from secrets.json
IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

//Initialize OPCClient
string serverAddress = config.GetConnectionString("serverAddress");
using var client = new OpcClient(serverAddress);
client.Connect();
string sbConnectionString = config.GetConnectionString("ServiceBus");
const string queueName = "myqueue";

await using ServiceBusClient serviceBusClient = new ServiceBusClient(sbConnectionString);
await using ServiceBusSender sender = serviceBusClient.CreateSender(queueName);
// Get all devices defined in secrets.json
IConfigurationSection devicesSection = config.GetSection("Devices");

ProductionLineManager productionLineManager = new ProductionLineManager(sender);
foreach (IConfigurationSection device in devicesSection.GetChildren())
{
    // Get device details
    string deviceConnectionString = device["DeviceConnectionString"];
    string deviceNodeId = device["DeviceNodeId"];
    Console.WriteLine(deviceConnectionString);

    //Initialize Device Client


    //Initialize Virtual Device
    productionLineManager.AddDevice(new VirtualDevice(deviceConnectionString,new NodeId(deviceNodeId), client));
    
    await Task.Delay(100);
}
Console.ReadLine();