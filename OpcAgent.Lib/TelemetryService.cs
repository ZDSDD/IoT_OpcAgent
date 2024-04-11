﻿using System.Net.Mime;
using System.Text;
using System.Timers;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using OpcAgent.Lib.Device;
using Timer = System.Timers.Timer;

namespace OpcAgent.Lib;

public class TelemetryService
{
    private readonly VirtualDevice _virtualDevice;

    private Timer _telemetryTimer;

    private readonly OpcRepository _repository;

    public TelemetryService(VirtualDevice virtualDevice, OpcRepository repository)
    {
        _virtualDevice = virtualDevice;
        InitTelemetryTimer();
        _repository = repository;
    }

    private async void SendTelemetryToCloud(TelemetryData data)
    {
        TelemetryData currentData = data;
        Message telemetryMessage = MessageService.PrepareMessage(currentData);
        await _virtualDevice.SendMessage(telemetryMessage);
    }

    private void OnTelemetryTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // Handle telemetry data transmission
        SendTelemetryToCloud(GetCurrentTelemetryData());
    }

    internal TelemetryData GetCurrentTelemetryData()
    {
        return new TelemetryData
        {
            ProductionStatus = _repository.GetProductionStatus(),
            WorkorderId = _repository.GetWorkerId(),
            GoodCount = _repository.GetGoodCount(),
            BadCount = _repository.GetBadCount(),
            Temperature = _repository.GetTemperature()
        };
    }

    private async void InitTelemetryTimer()
    {
        double telemetryInterval = await _virtualDevice.GetSendFrequency();
        _telemetryTimer = new Timer(ToMilliseconds(telemetryInterval)); // 20 seconds in milliseconds
        _telemetryTimer.Elapsed += OnTelemetryTimerElapsed;
        _telemetryTimer.AutoReset = true;
        _telemetryTimer.Start();
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
            Console.WriteLine($"Telemetry timer updated to: {newTelemetryTimeInSeconds} seconds");
        }
        else
        {
            Console.WriteLine($"Failed to set telemetry timer to: {newTelemetryTimeInSeconds} seconds");
        }
    }
}