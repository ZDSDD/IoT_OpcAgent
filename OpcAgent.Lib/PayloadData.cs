namespace OpcAgent.Lib;

public struct PayloadData
{
    public int ProductionStatus { get; set; }
    public string WorkorderId { get; set; }
    public long GoodCount { get; set; }
    public long BadCount { get; set; }
    public double Temperature { get; set; }
}