using OpcAgent.Lib.Managers.Mediator;

namespace OpcAgent.Lib.Managers;

public abstract class BaseManager
{
    protected IMediator _mediator;

    protected BaseManager(IMediator mediator = null)
    {
        _mediator = mediator;
    }

    public void SetMediator(IMediator mediator)
    {
        this._mediator = mediator;
    }
}