using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using OpcAgent.Lib.Enums;

namespace OpcAgent.Lib.Device;

public class VirtualDevice
{
    private readonly NodeId _nodeId;

    private const double DefaultSendFrequency = 60 * 5.0; //5 minutes
    private DeviceClient _deviceClient = null!;

    private TelemetryService _telemetryService = null!;
    public Action<string, int, bool> OnErrorsChange = null!;
    private readonly OpcClient _client;
    private OpcRepository _opcRepository = null!;
    private int _lastErrorsValue = 0;

    public VirtualDevice(string deviceConnectionString, NodeId nodeId, OpcClient opcClient)
    {
        _nodeId = nodeId;
        _client = opcClient;
        SetDeviceClient(deviceConnectionString);
        InitVirtualDevice();
    }

    ~VirtualDevice()
    {
        _deviceClient.DisposeAsync();
    }

    private async void SetDeviceClient(string deviceConnectionString)
    {
        _deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
        await _deviceClient.OpenAsync();
        _opcRepository = new OpcRepository(_client, OpcUtils.InitReadNodes(_nodeId));
        _telemetryService = new TelemetryService(this, _opcRepository);
    }

    #region Sending Messages

    public async Task SendMessage(Message message)
    {
        Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message. {_nodeId}");
        await _deviceClient.SendEventAsync(message);
        message.Dispose();
    }

    #endregion Sending Messages

    #region Receiving Messages

    private async Task OnC2dMessageReceivedAsync(Message receivedMessage, object _)
    {
        PrintMessage(receivedMessage);
        await _deviceClient.CompleteAsync(receivedMessage);
        receivedMessage.Dispose();
    }

    private void PrintMessage(Message receivedMessage)
    {
        string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
        Console.WriteLine($"\t{_nodeId}\tReceived message: {messageData}");

        int propCount = 0;
        foreach (var prop in receivedMessage.Properties)
        {
            Console.WriteLine($"\t\tProperty[{propCount++}> Key={prop.Key} : Value={prop.Value}");
        }
    }

    #endregion Receiving Messages

    #region Direct Methods

    private static async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");

        await Task.Delay(1000);

        return new MethodResponse(0);
    }

    #endregion Direct Methods

    #region Device Twin

    private async Task UpdateReportedDeviceTwinPropertyAsync(string propertyName, dynamic value)
    {
        var reportedProperties = new TwinCollection
        {
            [propertyName] = value
        };
        await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    }

    private Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
    {
        if (desiredProperties.Contains("ProductionRate"))
        {
            int desiredProductionRate = desiredProperties["ProductionRate"];
            SetProductionRate(desiredProductionRate);
        }

        if (desiredProperties.Contains("telemetryConfig"))
        {
            var telemetryConfig = desiredProperties["telemetryConfig"] as JObject;
            if (telemetryConfig != null && telemetryConfig.TryGetValue("sendFrequency", out var sendFrequency))
            {
                _telemetryService.SetTelemetryTime(
                    DeviceTwinPropertyParser.ConvertSendFrequencyToSeconds(sendFrequency.ToString()));
            }
        }

        return Task.CompletedTask;
    }

    #endregion Device Twin

    private async Task InitializeHandlers()
    {
        await _deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, _deviceClient);

        await _deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStopHandler, _deviceClient);

        await _deviceClient.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatusHandler, _deviceClient);
        await _deviceClient.SetMethodDefaultHandlerAsync(DefaultServiceHandler, _deviceClient);

        await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, _deviceClient);
    }

    private async void InitVirtualDevice()
    {
        await InitializeHandlers();
        try
        {
            await UpdateReportedDeviceTwinPropertyAsync("DeviceErrors", _opcRepository.GetErrors());
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        var twin = await _deviceClient.GetTwinAsync();
        if (twin.Properties.Desired.Contains("ProductionRate"))
        {
            int productionRate = twin.Properties.Desired["ProductionRate"];
            SetProductionRate(productionRate);
        }

        await UpdateReportedDeviceTwinPropertyAsync("ProductionRate", _opcRepository.GetProductionRate());
        _client.SubscribeDataChange($"{_nodeId}/{OpcEndpoint.DeviceError}", HandleErrorsChanged);
    }

    private void SetProductionRate(int productionRate)
    {
        OpcStatus result = _client.WriteNode($"{_nodeId}/{OpcEndpoint.ProductionRate}", productionRate);
        Console.WriteLine(result.IsGood
            ? $"{_nodeId}Production rate successfully changed to: {productionRate}"
            : $"{_nodeId}Could not change production rate.");
        _ = UpdateReportedDeviceTwinPropertyAsync("ProductionRate", _opcRepository.GetProductionRate());
    }

    // Send D2C message and handle errors change
    private async void HandleErrorsChanged(object sender, OpcDataChangeReceivedEventArgs e)
    {
        object errors = e.Item.Value.Value;
        int errorsValue = (int)errors;
        bool errorsIncreased = _lastErrorsValue < errorsValue;
        _lastErrorsValue = errorsValue;

        Message errorEventMessage = MessageService.PrepareMessage(new
        {
            Errors = errorsValue,
            DeviceNode = _nodeId.Identifier,
            Event = "error",
            ErrorsIncreased = errorsIncreased ? 1 : 0
        });
        await SendMessage(errorEventMessage);

        OnErrorsChange.Invoke(this._nodeId.ToString(), errorsValue, errorsIncreased);

        await UpdateReportedDeviceTwinPropertyAsync("DeviceErrors", (int)errors);
    }

    public async Task<double> GetSendFrequency()
    {
        var twin = await this._deviceClient.GetTwinAsync();
        var desiredProperties = twin.Properties.Desired;

        if (!desiredProperties.Contains("telemetryConfig")) return DefaultSendFrequency;

        var telemetryConfig = desiredProperties["telemetryConfig"] as JObject;

        if (telemetryConfig == null || !telemetryConfig.TryGetValue("sendFrequency", out var sendFrequency))
            return DefaultSendFrequency;
        try
        {
            return DeviceTwinPropertyParser.ConvertSendFrequencyToSeconds(sendFrequency.ToString());
        }
        catch (ArgumentException argumentException)
        {
            Console.WriteLine(argumentException.ToString());
        }

        return DefaultSendFrequency;
    }

    private Task<MethodResponse>? ResetErrorStatusHandler(MethodRequest methodRequest, object _)
    {
        try
        {
            _client.CallMethod(
                _nodeId,
                $"{_nodeId}/{OpcEndpoint.ResetErrorStatus}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"{_nodeId}: exception occured during ResetErrorStatus: {exception}");
            return Task.FromException(exception) as Task<MethodResponse>;
        }

        return Task.FromResult(new MethodResponse(0));
    }

    private Task<MethodResponse>? EmergencyStopHandler(MethodRequest methodRequest, object _)
    {
        try
        {
            _client.CallMethod(
                _nodeId,
                $"{_nodeId}/{OpcEndpoint.EmergencyStop}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"{_nodeId}: exception occured during emergency stop: {exception}");
            return Task.FromException(exception) as Task<MethodResponse>;
        }

        return Task.FromResult(new MethodResponse(0));
    }
}