using OpcAgent.Enums.Feature;
using OpcAgent.Lib;

namespace OpcAgent.Selector.Implementation;

public class ProductionLineFeatureSelector(ProductionLineManager manager) : SelectorBase
{
    public override void PrintMenu()
    {
        Console.WriteLine(@"
            1 - Log all info from device
            2 - Emergency stop");
    }

    public void Execute(ProductionLineFeature feature)
    {
        switch (feature)
        {
            case ProductionLineFeature.LogAll:
                manager.LogAllInfo();
                break;
            case ProductionLineFeature.EmergencyStop:
                manager.EmergencyStop();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(feature), feature, null);
        }
    }
}