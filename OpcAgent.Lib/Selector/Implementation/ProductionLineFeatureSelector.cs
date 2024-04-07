using OpcAgent.Enums.Feature;
using OpcAgent.Selector;

namespace OpcAgent.Lib.Selector.Implementation;

public class ProductionLineFeatureSelector(ProductionLineManager manager) : SelectorBase
{
    public override void PrintMenu()
    {
        Console.WriteLine("""

                          1 - Log all info from device
                          2 - Emergency stop
                          3 - Reset error status
                          4 - Update device errors
                          0 - Exit
                          """);
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
            case ProductionLineFeature.ResetErrorStatus:
                manager.ResetErrorStatus();
                break;
            case ProductionLineFeature.UpdateDeviceErrors:
                manager.UpdateDeviceErrors();
                break;
            case ProductionLineFeature.Exit:
            default:
                Console.WriteLine("Exiting the program");
                break;
        }
    }
}