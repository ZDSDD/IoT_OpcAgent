using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Opc.Ua;
using Opc.UaFx.Client;
using OpcAgent;
using OpcAgent.Enums.Feature;
using OpcAgent.Lib;
using OpcAgent.Lib.Device;
using OpcAgent.Lib.Managers;
using OpcAgent.Lib.Managers.Mediator;
using OpcAgent.Lib.Selector.Implementation;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

//Get keys from secrets.json
IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

//Initialize Device Client
string deviceConnectionString = config.GetConnectionString("Device1");
await using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
await deviceClient.OpenAsync();

//Initialize Virtual Device
var device = new VirtualDevice(deviceClient);

//Initialize OPCClient
string serverAddress = config.GetConnectionString("serverAddress");
using var client = new OpcClient(serverAddress);
client.Connect();

//Initialize Production Line Manager
NodeId nodeId = new NodeId("ns=2;s=Device 1");
ProductionLineManager productionLineManager = new ProductionLineManager(client, nodeId, device);

//Initialize IoT Hub Manager
string serviceConnectionString = config.GetConnectionString("IoTHub");
using var serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString);
using var registryManager = RegistryManager.CreateFromConnectionString(serviceConnectionString);
var iotHubManager = new IoTHubManager(serviceClient, registryManager);

//Initialize Mediator between Managers
new ManagersMediator(iotHubManager, productionLineManager);

//Initialize Selectors
ProductionLineFeatureSelector selector = new ProductionLineFeatureSelector(productionLineManager);

//Main menu
int input;
do
{
    selector.PrintMenu();
    input = selector.ReadInput();
    selector.Execute((ProductionLineFeature)input);
} while (input != 0);

//Close connections
client.Disconnect();
await deviceClient.CloseAsync();
await registryManager.CloseAsync();
await serviceClient.CloseAsync();
