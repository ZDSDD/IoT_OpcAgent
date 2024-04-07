namespace OpcAgent.Lib.Managers.Mediator;

public class ManagersMediator : IMediator
{
    private IoTHubManager _ioTHubManager;
    private ProductionLineManager _productionLineManager;

    public ManagersMediator(IoTHubManager ioTHubManager, ProductionLineManager productionLineManager)
    {
        _ioTHubManager = ioTHubManager;
        _ioTHubManager.SetMediator(this);
        _productionLineManager = productionLineManager;
        _productionLineManager.SetMediator(this);
    }

    public void Notify(object sender, string ev)
    {
        throw new NotImplementedException();
    }
}