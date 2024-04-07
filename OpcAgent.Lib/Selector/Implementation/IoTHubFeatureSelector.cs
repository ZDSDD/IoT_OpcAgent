using Microsoft.Azure.Devices.Common.Exceptions;
using OpcAgent.Enums.Feature;
using OpcAgent.Lib.Managers;
using OpcAgent.Selector;

namespace OpcAgent.Lib.Selector.Implementation;

internal class IoTHubFeatureSelector(IoTHubManager manager) : SelectorBase
{
    public override void PrintMenu()
    {
        Console.WriteLine("""
                          
                              1 - C2D
                              2 - Direct Method
                              3 - Device Twin
                              0 - Exit
                          """);
    }

    public async Task Execute(IoTHubFeature ioTHubFeature)
    {
        switch (ioTHubFeature)
        {
            case IoTHubFeature.C2D:
            {
                Console.WriteLine("\nType your message (confirm with enter):");
                string messageText = Console.ReadLine() ?? string.Empty;

                Console.WriteLine("Type your device ID (confirm with enter):");
                string deviceId = Console.ReadLine() ?? string.Empty;

                await manager.SendMessage(messageText, deviceId);

                Console.WriteLine("Message sent!");
            }
                break;
            case IoTHubFeature.DirectMethod:
            {
                Console.WriteLine("\nType your device ID (confirm with enter):");
                string deviceId = Console.ReadLine() ?? string.Empty;
                try
                {
                    var result = await manager.ExecuteDeviceMethod("SendMessages", deviceId);
                    Console.WriteLine($"Method executed with status {result}");
                }
                catch (DeviceNotFoundException)
                {
                    Console.WriteLine("Device not connected!");
                }
            }
                break;
            case IoTHubFeature.DeviceTwin:
            {
                Console.WriteLine("\nType property name (confirm with enter):");
                string propertyName = Console.ReadLine() ?? string.Empty;

                Console.WriteLine("\nType your device ID (confirm with enter):");
                string deviceId = Console.ReadLine() ?? string.Empty;

                var random = new Random();
                await manager.UpdateDesiredTwin(deviceId, propertyName, random.Next());
            }
                break;
        }
    }
}