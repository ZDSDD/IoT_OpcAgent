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

public class OpcRepository(OpcClient client, Dictionary<OpcEndpoint, OpcReadNode> readValuesCommands)
    : IOpcRepository
{
    public int TotalMethods { get; private set; } = 7;

    public T GetValue<T>(OpcEndpoint endpoint)
    {
        try
        {
            var result = client.ReadNode(readValuesCommands[endpoint]);
            if (result.Status.IsGood)
            {
                return (T)result.Value;
            }
            throw new OpcRepositoryException("Error reading endpoint: "+ endpoint);
        }
        catch (Exception exception)
        {
            throw;
        }
    }

    internal int GetProductionStatus()
    {
        try
        {
            return GetValue<int>(OpcEndpoint.ProductionStatus);
        }
        catch (Exception ex)
        {
            throw; // Rethrow the exception
        }
    }

    internal string GetWorkerId()
    {
        try
        {
            return GetValue<string>(OpcEndpoint.WorkorderId);
        }
        catch (Exception ex)
        {
            throw; // Rethrow the exception
        }
    }

    internal long GetGoodCount()
    {
        try
        {
            return GetValue<long>(OpcEndpoint.GoodCount);
        }
        catch (Exception ex)
        {
            throw; // Rethrow the exception
        }
    }

    internal long GetBadCount()
    {
        try
        {
            return GetValue<long>(OpcEndpoint.BadCount);
        }
        catch (Exception ex)
        {
            throw; // Rethrow the exception
        }
    }

    internal double GetTemperature()
    {
        try
        {
            return GetValue<double>(OpcEndpoint.Temperature);
        }
        catch (Exception ex)
        {
            throw; // Rethrow the exception
        }
    }

    internal int GetProductionRate()
    {
        try
        {
            return GetValue<int>(OpcEndpoint.ProductionRate);
        }
        catch (Exception ex)
        {
            throw; // Rethrow the exception
        }
    }

    internal int GetErrors()
    {
        try
        {
            return GetValue<int>(OpcEndpoint.DeviceError);
        }
        catch (Exception ex)
        {
            throw; // Rethrow the exception
        }
    }

    public bool CheckConnection()
    {
        try
        {
            GetWorkerId();
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }
}

public interface IOpcRepository
{
    T GetValue<T>(OpcEndpoint endpoint);
}