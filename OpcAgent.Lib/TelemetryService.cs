using System.Net.Mime;
using System.Text;
using System.Timers;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.UaFx;
using OpcAgent.Lib.Device;
using Timer = System.Timers.Timer;

namespace OpcAgent.Lib;

public class TelemetryService : IDisposable
{
    private readonly VirtualDevice _virtualDevice;

    private Timer _telemetryTimer = null!;

    private readonly OpcRepository _repository;

    private double _defaultTelemetryInterval;

    public TelemetryService(VirtualDevice virtualDevice, OpcRepository repository)
    {
        _virtualDevice = virtualDevice;
        InitializeTelemetryTimer();
        _repository = repository;
        _defaultTelemetryInterval = 10;
    }

    public void StopTelemetry()
    {
        this._telemetryTimer.Stop();
        Console.WriteLine("Telemetry timer stopped successfully");
    }

    public void StartTelemetry()
    {
        this._telemetryTimer.Start();
        Console.WriteLine("Telemetry timer started successfully");
    }

    private async void SendTelemetryToCloud(TelemetryData data)
    {
        TelemetryData currentData = data;
        Message telemetryMessage = MessageService.PrepareMessage(currentData);
        await _virtualDevice.SendMessage(telemetryMessage);
    }

    private void OnTelemetryTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // Handle telemetry data transmission
        try
        {
            SendTelemetryToCloud(GetCurrentTelemetryData());
        }
        catch (OpcException)
        {
            Console.WriteLine(@"Coudln't send telemetry to the cloud");
        }
    }

    private long _lastTotalGoodCount = 0;
    private long _lastTotalBadCount = 0;

    private TelemetryData GetCurrentTelemetryData()
    {
        try
        {
            long totalGoodCount = _repository.GetGoodCount();
            if (totalGoodCount < _lastTotalGoodCount) _lastTotalGoodCount = 0;
            long goodCount = totalGoodCount - _lastTotalGoodCount;

            long totalBadCount = _repository.GetBadCount();
            if (totalBadCount < _lastTotalBadCount) _lastTotalBadCount = 0;
            long badCount = totalBadCount - _lastTotalBadCount;

            _lastTotalGoodCount = totalGoodCount;
            _lastTotalBadCount = totalBadCount;

            return new TelemetryData
            {
                ProductionStatus = _repository.GetProductionStatus(),
                WorkorderId = _repository.GetWorkerId(),
                GoodCount = goodCount,
                BadCount = badCount,
                TotalGoodCount = totalGoodCount,
                TotalBadCount = totalBadCount,
                Temperature = _repository.GetTemperature()
            };
        }
        catch (OpcRepositoryException exception)
        {
            Console.WriteLine(exception);
            Console.WriteLine(@"Check if device is running");
        }
        catch (OpcException exception)
        {
            Console.WriteLine("Exception occured during telemetry service: " + exception.Message);
            Console.WriteLine("Stoping the timer");
            this._telemetryTimer.Stop();
            throw;
        }

        return new TelemetryData{};
    }

    private async void InitializeTelemetryTimer()
    {
        try
        {
            double telemetryInterval = await _virtualDevice.GetSendFrequency();
            _telemetryTimer = new Timer(ToMilliseconds(telemetryInterval));
            _telemetryTimer.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            _telemetryTimer = new Timer(ToMilliseconds(_defaultTelemetryInterval));
            _telemetryTimer.Start();
        }
        finally
        {
            _telemetryTimer.Elapsed += OnTelemetryTimerElapsed;
            _telemetryTimer.AutoReset = true;
        }
    }

    private double ToMilliseconds(double seconds)
    {
        return seconds * 1000.0;
    }

    internal void SetTelemetryTime(double newTelemetryTimeInSeconds)
    {
        //Do not allow telemetry time to be really small
        if (newTelemetryTimeInSeconds > 2)
        {
            _telemetryTimer.Interval = ToMilliseconds(newTelemetryTimeInSeconds);
            Console.WriteLine($@"Telemetry timer updated to: {newTelemetryTimeInSeconds} seconds");
        }
        else
        {
            Console.WriteLine($@"Failed to set telemetry timer to: {newTelemetryTimeInSeconds} seconds");
        }
    }

    public void Dispose()
    {
        _telemetryTimer.Dispose();
    }
}