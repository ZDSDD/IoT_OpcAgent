using Microsoft.Extensions.Configuration;
using Opc.Ua;
using Opc.UaFx.Client;
using OpcAgent;
using OpcAgent.Enums.Feature;
using OpcAgent.Lib;
using OpcAgent.Selector.Implementation;


IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
string serverAddress = config.GetConnectionString("serverAddress");
// string serviceConnectionString = config["IoTHub"];
// using var serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString);
// using var registryManager = RegistryManager.CreateFromConnectionString(serviceConnectionString);


using var client = new OpcClient(serverAddress);

client.Connect();
NodeId nodeId = new NodeId("ns=2;s=Device 1");

ProductionLineManager productionLineManager = new ProductionLineManager(client, nodeId);

ProductionLineFeatureSelector selector = new ProductionLineFeatureSelector(productionLineManager);
int input;
do
{
    selector.PrintMenu();
    input = selector.ReadInput();
    selector.Execute((ProductionLineFeature)input);
} while (input != 0);

client.Disconnect();