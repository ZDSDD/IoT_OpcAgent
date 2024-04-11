using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using OpcAgent.Lib.Device;
using OpcAgent.Lib.Enums;
using System.Timers;
using Microsoft.Azure.Devices.Client;
using Timer = System.Timers.Timer;

namespace OpcAgent.Lib.Managers;

public class ProductionLineManager : BaseManager
{
    private readonly OpcClient _client;
    private readonly NodeId _nodeId;
    private readonly VirtualDevice _virtualDevice;
    private readonly Dictionary<OpcEndpoint, OpcReadNode> _readValuesCommands;
    private readonly Dictionary<OpcEndpoint, OpcReadNode> _readAttributeCommands;
    private readonly OpcSubscription _errorSubscription;
    private readonly TelemetryService _telemetryService;
    private readonly OpcRepository _opcRepository;

    public ProductionLineManager(OpcClient client, NodeId nodeId, VirtualDevice virtualDevice)
    {
        _client = client;
        _nodeId = nodeId;
        _virtualDevice = virtualDevice;
        _readValuesCommands = OpcUtils.InitReadNodes(this._nodeId);
        _readAttributeCommands = OpcUtils.InitReadNameNodes(this._nodeId);
        _opcRepository = new OpcRepository(_client, _readValuesCommands);
        _errorSubscription = client.SubscribeDataChange($"{nodeId}/{OpcEndpoint.DeviceError}", HandleErrorsChanged);
        InitVirtualDevice();
        _telemetryService = new TelemetryService(virtualDevice, _opcRepository);
    }

    private async void InitVirtualDevice()
    {
        await _virtualDevice.InitializeHandlers();
        _virtualDevice.EmergencyStopRequested += EmergencyStop;
        _virtualDevice.ResetErrorStatusRequested += ResetErrorStatus;
        _virtualDevice.DesiredProductionRateChanged += SetProductionRate;
        _virtualDevice.DesiredSendFrequencyChanged += _telemetryService.SetTelemetryTime;
        try
        {
            await _virtualDevice.UpdateErrorsAsync(_opcRepository.GetErrors());
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        await _virtualDevice.UpdateProductionRateAsync(_opcRepository.GetProductionRate());
    }

    private void SetProductionRate(int productionRate)
    {
        OpcStatus result = _client.WriteNode($"{_nodeId}/{OpcEndpoint.ProductionRate}", productionRate);
        if (result.IsGood)
        {
            Console.WriteLine("Production rate successfully changed");
        }
    }

    private async void HandleErrorsChanged(object sender, OpcDataChangeReceivedEventArgs e)
    {
        OpcMonitoredItem item = (OpcMonitoredItem)sender;
        object errors = e.Item.Value.Value;

        //send D2C message
        Message errorEventMessage = MessageService.PrepareMessage(this._telemetryService.GetCurrentTelemetryData());
        errorEventMessage.Properties.Add("ErrorEvent", "true");
        await _virtualDevice.SendMessage(errorEventMessage);

        //update device twin
        await _virtualDevice.UpdateErrorsAsync((int)errors);
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