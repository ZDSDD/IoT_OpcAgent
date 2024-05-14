namespace OpcAgent.Lib;

public struct TelemetryData
{
    public int ProductionStatus { get; set; }
    public string WorkorderId { get; set; }
    public long GoodCount { get; set; }
    public long BadCount { get; set;  }
    public long TotalGoodCount { get; set; }
    public long TotalBadCount { get; set; }
    public double Temperature { get; set; }
}