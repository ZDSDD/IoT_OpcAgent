using System.Diagnostics;

namespace OpcAgent.Lib;

public struct EventData
{
    public string EventType { get; set; }
    public string Message { get; set; }
}