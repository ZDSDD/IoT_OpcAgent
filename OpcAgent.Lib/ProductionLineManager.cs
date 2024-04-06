using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;

namespace OpcAgent.Lib;

public class ProductionLineManager
{
    private readonly OpcClient _client;
    private readonly NodeId _nodeId;

    private readonly Dictionary<OpcEndpoint, OpcReadNode> _readValuesCommands;
    private readonly Dictionary<OpcEndpoint, OpcReadNode> _readAttributeCommands;

    public ProductionLineManager(OpcClient client, NodeId nodeId)
    {
        _client = client;
        _nodeId = nodeId;
        _readValuesCommands = OpcUtils.InitReadNodes(this._nodeId);
        _readAttributeCommands = OpcUtils.InitReadNameNodes(this._nodeId);
    }

    public void EmergencyStop()
    {
        object[] emergencyStopResult = _client.CallMethod(
            _nodeId,
            $"{_nodeId}/{OpcEndpoint.EmergencyStop}");
    }
    
    public void LogAllInfo()
    {
        IEnumerable<OpcValue> job = _client.ReadNodes(_readValuesCommands.Values.ToArray());
        IEnumerable<OpcValue> jobName = _client.ReadNodes(_readAttributeCommands.Values.ToArray());
        var valuesAndNames = job.Zip(jobName, (first, second) => second + ": " + first);
        Console.WriteLine();
        foreach (var item in valuesAndNames)
        {
            Console.WriteLine(item);
        }
    }
}