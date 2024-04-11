using Opc.UaFx;
using Opc.UaFx.Client;
using OpcAgent.Lib.Enums;

namespace OpcAgent.Lib;

public class OpcRepository : IOpcRepository
{
    private readonly OpcClient _client;
    private readonly Dictionary<OpcEndpoint, OpcReadNode> _readValuesCommands;

    public OpcRepository(OpcClient client, Dictionary<OpcEndpoint, OpcReadNode> readValuesCommands)
    {
        _client = client;
        _readValuesCommands = readValuesCommands;
    }

    public T GetValue<T>(OpcEndpoint endpoint)
    {
        var result = _client.ReadNode(_readValuesCommands[endpoint]);
        if (result.Status.IsGood)
        {
            return (T)result.Value;
        }

        throw new Exception("Error reading node: " + result.Status);
    }

    internal int GetProductionStatus()
    {
        return GetValue<int>(OpcEndpoint.ProductionStatus);
    }

    internal string GetWorkerId()
    {
        return GetValue<string>(OpcEndpoint.WorkorderId);
    }

    internal long GetGoodCount()
    {
        return GetValue<long>(OpcEndpoint.GoodCount);
    }

    internal long GetBadCount()
    {
        return GetValue<long>(OpcEndpoint.BadCount);
    }

    internal double GetTemperature()
    {
        return GetValue<double>(OpcEndpoint.Temperature);
    }

    internal int GetProductionRate()
    {
        return GetValue<int>(OpcEndpoint.ProductionRate);
    }

    internal int GetErrors()
    {
        return GetValue<int>(OpcEndpoint.DeviceError);
    }
}

public interface IOpcRepository
{
    T GetValue<T>(OpcEndpoint endpoint);
}