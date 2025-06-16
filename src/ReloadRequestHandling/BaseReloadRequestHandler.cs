using System;
using System.Collections.Generic;
using Bloodpebble.ReloadExecution;
using Bloodpebble.ReloadRequesting;


namespace Bloodpebble.ReloadRequestHandling;

internal abstract class BaseReloadRequestHandler : IReloadRequestHandler
{
    protected IPluginLoader PluginLoader { get; }
    private List<IReloadRequestor> _subscriptions = [];

    public event EventHandler<FullReloadStartingEventArgs>? FullReloadStarting;
    public event EventHandler<PartialReloadStartingEventArgs>? PartialReloadStarting;
    public event EventHandler<SoftReloadStartingEventArgs>? SoftReloadStarting;

    protected BaseReloadRequestHandler(IPluginLoader pluginLoader)
    {
        PluginLoader = pluginLoader;
    }

    public void Subscribe(IReloadRequestor requestor)
    {
        _subscriptions.Add(requestor);
        requestor.FullReloadRequested += HandleFullReloadRequested;
        requestor.PartialReloadRequested += HandlePartialReloadRequested;
        requestor.SoftReloadRequested += HandleSoftReloadRequested;
    }

    public void Unsubscribe()
    {
        foreach (var requestor in _subscriptions)
        {
            requestor.FullReloadRequested -= HandleFullReloadRequested;
            requestor.PartialReloadRequested -= HandlePartialReloadRequested;
            requestor.SoftReloadRequested -= HandleSoftReloadRequested;
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

    public void HandleSoftReloadRequested(object? sender, SoftReloadRequestedEventArgs ev)
    {
        HandleSoftReloadRequested(ev.Request);
    }

    virtual public void Dispose()
    {
        Unsubscribe();
    }

    protected void OnFullReloadStarting(IEnumerable<FullReloadRequest> fullReloadRequests, IEnumerable<PartialReloadRequest> partialReloadRequests, IEnumerable<SoftReloadRequest> softReloadRequests)
    {
        FullReloadStarting?.Invoke(this, new FullReloadStartingEventArgs(
            requestHandler: this,
            pluginLoader: PluginLoader,
            fullReloadRequests,
            partialReloadRequests,
            softReloadRequests
        ));
    }

    protected void OnPartialReloadStarting(IEnumerable<PartialReloadRequest> partialReloadRequests, ISet<string> allRequestedPluginGuids)
    {
        PartialReloadStarting?.Invoke(this, new PartialReloadStartingEventArgs(
            requestHandler: this,
            pluginLoader: PluginLoader,
            partialReloadRequests,
            allRequestedPluginGuids
        ));
    }

    protected void OnSoftReloadStarting(IEnumerable<SoftReloadRequest> softReloadRequests)
    {
        SoftReloadStarting?.Invoke(this, new SoftReloadStartingEventArgs(
            requestHandler: this,
            pluginLoader: PluginLoader,
            softReloadRequests
        ));
    }

    public abstract void HandleFullReloadRequested(FullReloadRequest request);
    public abstract void HandlePartialReloadRequested(PartialReloadRequest request);
    public abstract void HandleSoftReloadRequested(SoftReloadRequest request);
    public abstract void Update();

    protected static ReloadResultStatus PartialReloadResultStatus(bool faulted, ISet<string> requestedGuids, ISet<string> reloadedGuids)
    {
        if (faulted)
        {
            return ReloadResultStatus.Faulted;
        }
        else if (!requestedGuids.IsSubsetOf(reloadedGuids))
        {
            return ReloadResultStatus.PartialSuccess;
        }
        return ReloadResultStatus.Success;
    }

}
