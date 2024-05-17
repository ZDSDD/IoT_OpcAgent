using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using OpcAgent.Lib.Enums;

namespace OpcAgent.Lib;

public static class OpcUtils
{
    public static Dictionary<OpcEndpoint, OpcReadNode> InitReadNodes(NodeId nodeId)
    {
        return new Dictionary<OpcEndpoint, OpcReadNode>
        {
            {OpcEndpoint.ProductionStatus, new OpcReadNode($"{nodeId}/{OpcEndpoint.ProductionStatus}") },
            {OpcEndpoint.ProductionRate, new OpcReadNode($"{nodeId}/{OpcEndpoint.ProductionRate}") },
            {OpcEndpoint.WorkorderId, new OpcReadNode($"{nodeId}/{OpcEndpoint.WorkorderId}") },
            {OpcEndpoint.Temperature, new OpcReadNode($"{nodeId}/{OpcEndpoint.Temperature}") },
            {OpcEndpoint.GoodCount, new OpcReadNode($"{nodeId}/{OpcEndpoint.GoodCount}") },
            {OpcEndpoint.BadCount, new OpcReadNode($"{nodeId}/{OpcEndpoint.BadCount}") },
            {OpcEndpoint.DeviceError, new OpcReadNode($"{nodeId}/{OpcEndpoint.DeviceError}") }
        };
    }

    internal static Dictionary<OpcEndpoint, OpcReadNode> InitReadNameNodes(NodeId nodeId)
    {
        return new Dictionary<OpcEndpoint, OpcReadNode>
        {
            {OpcEndpoint.ProductionStatus, new OpcReadNode($"{nodeId}/{OpcEndpoint.ProductionStatus}", OpcAttribute.DisplayName)},
            {OpcEndpoint.ProductionRate, new OpcReadNode($"{nodeId}/{OpcEndpoint.ProductionRate}", OpcAttribute.DisplayName)},
            {OpcEndpoint.WorkorderId, new OpcReadNode($"{nodeId}/{OpcEndpoint.WorkorderId}", OpcAttribute.DisplayName)},
            {OpcEndpoint.Temperature, new OpcReadNode($"{nodeId}/{OpcEndpoint.Temperature}", OpcAttribute.DisplayName)},
            {OpcEndpoint.GoodCount, new OpcReadNode($"{nodeId}/{OpcEndpoint.GoodCount}", OpcAttribute.DisplayName)},
            {OpcEndpoint.BadCount, new OpcReadNode($"{nodeId}/{OpcEndpoint.BadCount}", OpcAttribute.DisplayName)},
            {OpcEndpoint.DeviceError, new OpcReadNode($"{nodeId}/{OpcEndpoint.DeviceError}", OpcAttribute.DisplayName)}
        };
    }
}