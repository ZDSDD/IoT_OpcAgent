namespace OpcAgent.Lib.Managers.Mediator;

public interface IMediator
{
    void Notify(object sender, string ev);
}