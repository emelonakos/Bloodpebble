using System;
using System.Collections.Generic;
using Bloodpebble.ReloadRequesting;


namespace Bloodpebble.ReloadRequestHandling;

internal abstract class BaseReloadRequestHandler : IReloadRequestHandler
{
    private List<IReloadRequestor> _subscriptions = [];

    public void Subscribe(IReloadRequestor requestor)
    {
        _subscriptions.Add(requestor);
        requestor.FullReloadRequested += HandleFullReloadRequested;
        requestor.PartialReloadRequested += HandlePartialReloadRequested;
    }

    public void Unsubscribe()
    {
        foreach (var requestor in _subscriptions)
        {
            requestor.FullReloadRequested -= HandleFullReloadRequested;
            requestor.PartialReloadRequested -= HandlePartialReloadRequested;
        }
        _subscriptions.Clear();
    }

    public void HandleFullReloadRequested(object? sender, FullReloadRequestedEventArgs ev)
    {
        HandleFullReloadRequested(ev.Request);
    }

    public void HandlePartialReloadRequested(object? sender, PartialReloadRequestedEventArgs ev)
    {
        HandlePartialReloadRequested(ev.Request);
    }

    virtual public void Dispose()
    {
        Unsubscribe();
    }

    public abstract void HandleFullReloadRequested(FullReloadRequest request);
    public abstract void HandlePartialReloadRequested(PartialReloadRequest request);
    public abstract void Update();
    
}
