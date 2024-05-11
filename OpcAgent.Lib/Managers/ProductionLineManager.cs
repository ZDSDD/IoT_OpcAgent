using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using OpcAgent.Lib.Device;
using OpcAgent.Lib.Enums;
using Microsoft.Azure.Devices.Client;

namespace OpcAgent.Lib.Managers;

public class ProductionLineManager
{
    private List<VirtualDevice> _devices = [];

    public void AddDevice(VirtualDevice virtualDevice)
    {
        _devices.Add(virtualDevice);
    }
}