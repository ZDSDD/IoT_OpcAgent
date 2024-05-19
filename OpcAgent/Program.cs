using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using OpcAgent.Lib;
using OpcAgent.Lib.Device;
using OpcAgent.Lib.Managers;
using OpcAgent.Lib.Selector;
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
    opcClient.ReconnectTimeout = 3000;
    opcClient.OperationTimeout = 2000;
    opcClient.Connected += (sender, eventArgs) => { Console.WriteLine(@"connecteed! :)"); };
    opcClient.StateChanged += (sender, eventArgs) =>
    {
        Console.WriteLine($@"State changed: {eventArgs.OldState} to {eventArgs.NewState}");
    };
    opcClient.Connect();

    opcClient.Reconnected += (sender, eventArgs) => { Console.WriteLine(@"opc client reconnected successfully"); };

    opcClient.Reconnecting += (sender, eventArgs) =>
    {
        Console.WriteLine(@"Lost connection... trying to reconnect..." + eventArgs);
    };
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
    //Initialize Device Client
    await FeatureSelector.AddDevice(deviceNodeId, opcClient, deviceConnectionString, productionLineManager);
}


int input;
do
{
    FeatureSelector.PrintMenu(productionLineManager);
    input = FeatureSelector.ReadInput();
    await FeatureSelector.Execute(input, productionLineManager, opcClient);
} while (input != 0);
