using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using OpcAgent.Lib.Device;
using OpcAgent.Lib.Enums;

namespace OpcAgent.Lib.Managers;

public class ProductionLineManager : BaseManager
{
    private readonly OpcClient _client;
    private readonly NodeId _nodeId;
    private readonly VirtualDevice _virtualDevice;

    private readonly Dictionary<OpcEndpoint, OpcReadNode> _readValuesCommands;
    private readonly Dictionary<OpcEndpoint, OpcReadNode> _readAttributeCommands;
    private readonly OpcSubscription _errorSubscription;

    public ProductionLineManager(OpcClient client, NodeId nodeId, VirtualDevice virtualDevice)
    {
        _client = client;
        _nodeId = nodeId;
        _virtualDevice = virtualDevice;
        _readValuesCommands = OpcUtils.InitReadNodes(this._nodeId);
        _readAttributeCommands = OpcUtils.InitReadNameNodes(this._nodeId);
        _errorSubscription = client.SubscribeDataChange($"{nodeId}/{OpcEndpoint.DeviceError}", HandleErrorsChanged);

        InitVirtualDevice();
    }

    private async void InitVirtualDevice()
    {
        await _virtualDevice.InitializeHandlers();
        _virtualDevice.EmergencyStopRequested += EmergencyStop;
        _virtualDevice.ResetErrorStatusRequested += ResetErrorStatus;
        _virtualDevice.DesiredPropertyChangedRequested += SetProductionRate;
        await _virtualDevice.UpdateErrorsAsync(GetErrors());
        await _virtualDevice.UpdateProductionRateAsync(GetProductionRate());
    }

    private void SetProductionRate(int productionRate)
    {
        OpcStatus result = _client.WriteNode($"{_nodeId}/{OpcEndpoint.ProductionRate}", productionRate);
        if (result.IsGood)
        {
            Console.WriteLine("Production rate successfully changed");
        }
    }

    //todo:
    //  - single D2C message sent to IoT platform
    //  - current value must be stored in the Reported Device Twin
    private async void HandleErrorsChanged(object sender, OpcDataChangeReceivedEventArgs e)
    {
        OpcMonitoredItem item = (OpcMonitoredItem)sender;
        object errors = e.Item.Value.Value;
        Console.WriteLine(
            "Data Change from NodeId '{0}': {1}",
            item.NodeId,
            (DeviceError)errors);

        //send D2C message
        await _virtualDevice.SendMessage(new PayloadData
        {
            ProductionStatus = GetProductionStatus(),
            WorkorderId = GetWorkerId(),
            GoodCount = GetGoodCount(),
            BadCount = GetBadCount(),
            Temperature = GetTemperature()
        }, (int)errors);
        
        //update device twin
        await _virtualDevice.UpdateErrorsAsync((int)errors);
    }

    private int GetProductionStatus()
    {
        return (int)_client.ReadNode(_readValuesCommands[OpcEndpoint.ProductionStatus]).Value;
    }

    private string GetWorkerId()
    {
        return (string)_client.ReadNode(_readValuesCommands[OpcEndpoint.WorkorderId]).Value;
    }

    private long GetGoodCount()
    {
        return (long)_client.ReadNode(_readValuesCommands[OpcEndpoint.GoodCount]).Value;
    }

    private long GetBadCount()
    {
        return (long)_client.ReadNode(_readValuesCommands[OpcEndpoint.BadCount]).Value;
    }

    private double GetTemperature()
    {
        return (double)_client.ReadNode(_readValuesCommands[OpcEndpoint.Temperature]).Value;
    }

    private int GetProductionRate()
    {
        return (int)_client.ReadNode(_readValuesCommands[OpcEndpoint.ProductionRate]).Value;
    }

    private int GetErrors()
    {
        return (int)_client.ReadNode(_readValuesCommands[OpcEndpoint.DeviceError]).Value;
    }

    public void UpdateDeviceErrors()
    {
        var a = _client.ReadNode(_readValuesCommands[OpcEndpoint.DeviceError]);
        Console.WriteLine($"Current errors are: {(DeviceError)a.Value}");
    }

    public void EmergencyStop()
    {
        object[] emergencyStopResult = _client.CallMethod(
            _nodeId,
            $"{_nodeId}/{OpcEndpoint.EmergencyStop}");
    }

    public void ResetErrorStatus()
    {
        object[] result = _client.CallMethod(
            _nodeId,
            $"{_nodeId}/{OpcEndpoint.ResetErrorStatus}");
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