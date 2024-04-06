using Microsoft.Extensions.Configuration;
using Opc.UaFx;
using Opc.UaFx.Client;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
string serverAddress = config.GetConnectionString("serverAddress");


using (var client = new OpcClient(serverAddress))
{
    client.Connect();

    OpcReadNode[] commands = new OpcReadNode[] {
    new OpcReadNode("ns=2;s=Device 1/ProductionStatus", OpcAttribute.DisplayName),
    new OpcReadNode("ns=2;s=Device 1/ProductionStatus"),
    new OpcReadNode("ns=2;s=Device 1/ProductionRate", OpcAttribute.DisplayName),
    new OpcReadNode("ns=2;s=Device 1/ProductionRate"),
    new OpcReadNode("ns=2;s=Device 1/WorkorderId", OpcAttribute.DisplayName),
    new OpcReadNode("ns=2;s=Device 1/WorkorderId"),
    new OpcReadNode("ns=2;s=Device 1/Temperature", OpcAttribute.DisplayName),
    new OpcReadNode("ns=2;s=Device 1/Temperature"),
    new OpcReadNode("ns=2;s=Device 1/GoodCount", OpcAttribute.DisplayName),
    new OpcReadNode("ns=2;s=Device 1/GoodCount"),
    new OpcReadNode("ns=2;s=Device 1/BadCount", OpcAttribute.DisplayName),
    new OpcReadNode("ns=2;s=Device 1/BadCount"),
    new OpcReadNode("ns=2;s=Device 1/DeviceError", OpcAttribute.DisplayName),
    new OpcReadNode("ns=2;s=Device 1/DeviceError"),
};

    IEnumerable<OpcValue> job = client.ReadNodes(commands);

    foreach (var item in job)
    {
        Console.WriteLine(item.Value);
    }
}