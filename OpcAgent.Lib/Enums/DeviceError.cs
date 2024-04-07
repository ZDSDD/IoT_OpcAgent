namespace OpcAgent.Lib.Enums;

[Flags]
public enum DeviceError
{
    None = 0b_0000,
    EmergencyStop = 0b_0001,
    PowerFailure = 0b_0010,
    SensorFailure = 0b_0100,
    Unknown = 0b_1000
}