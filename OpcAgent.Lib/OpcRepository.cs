using Opc.UaFx;
using Opc.UaFx.Client;
using OpcAgent.Lib.Enums;

namespace OpcAgent.Lib;

public class OpcRepositoryException : Exception
{
    public OpcRepositoryException()
    {
    }
    public OpcRepositoryException(string? message) : base(message)
    {
    }
    public OpcRepositoryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

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

        throw new OpcRepositoryException("Error reading node: " + result.Status);
    }

    internal int GetProductionStatus()
    {
        try
        {
            return GetValue<int>(OpcEndpoint.ProductionStatus);
        }
        catch (OpcRepositoryException ex)
        {
            Console.WriteLine($"Error getting production status: {ex.Message}");
            throw; // Rethrow the exception
        }
    }

    internal string GetWorkerId()
    {
        try
        {
            return GetValue<string>(OpcEndpoint.WorkorderId);
        }
        catch (OpcRepositoryException ex)
        {
            Console.WriteLine($"Error getting worker ID: {ex.Message}");
            throw; // Rethrow the exception
        }
    }

    internal long GetGoodCount()
    {
        try
        {
            return GetValue<long>(OpcEndpoint.GoodCount);
        }
        catch (OpcRepositoryException ex)
        {
            Console.WriteLine($"Error getting good count: {ex.Message}");
            throw; // Rethrow the exception
        }
    }

    internal long GetBadCount()
    {
        try
        {
            return GetValue<long>(OpcEndpoint.BadCount);
        }
        catch (OpcRepositoryException ex)
        {
            Console.WriteLine($"Error getting bad count: {ex.Message}");
            throw; // Rethrow the exception
        }
    }

    internal double GetTemperature()
    {
        try
        {
            return GetValue<double>(OpcEndpoint.Temperature);
        }
        catch (OpcRepositoryException ex)
        {
            Console.WriteLine($"Error getting temperature: {ex.Message}");
            throw; // Rethrow the exception
        }
    }

    internal int GetProductionRate()
    {
        try
        {
            return GetValue<int>(OpcEndpoint.ProductionRate);
        }
        catch (OpcRepositoryException ex)
        {
            Console.WriteLine($"Error getting production rate: {ex.Message}");
            throw; // Rethrow the exception
        }
    }

    internal int GetErrors()
    {
        try
        {
            return GetValue<int>(OpcEndpoint.DeviceError);
        }
        catch (OpcRepositoryException ex)
        {
            Console.WriteLine($"Error getting device errors: {ex.Message}");
            throw; // Rethrow the exception
        }
    }
}

public interface IOpcRepository
{
    T GetValue<T>(OpcEndpoint endpoint);
}