using System;
using System.Collections.Generic;
using Bloodpebble.ReloadExecution;

namespace Bloodpebble.ReloadRequesting;

internal interface IReloadRequestHandler : IReloadRequestSubscriber
{
    public event EventHandler<FullReloadStartingEventArgs>? FullReloadStarting;
    public event EventHandler<PartialReloadStartingEventArgs>? PartialReloadStarting;
    public event EventHandler<SoftReloadStartingEventArgs>? SoftReloadStarting;

    public void Dispose();
    public void Update();
    public void HandleFullReloadRequested(FullReloadRequest request);
    public void HandleFullReloadRequested(object? sender, FullReloadRequestedEventArgs ev);
    public void HandlePartialReloadRequested(PartialReloadRequest request);
    public void HandlePartialReloadRequested(object? sender, PartialReloadRequestedEventArgs ev);
    public void HandleSoftReloadRequested(SoftReloadRequest request);
    public void HandleSoftReloadRequested(object? sender, SoftReloadRequestedEventArgs ev);
}

internal interface IReloadRequestSubscriber
{
    public void Subscribe(IReloadRequestor requestor);
    public void Unsubscribe();
}


internal class FullReloadStartingEventArgs(
    IReloadRequestHandler requestHandler,
    IPluginLoader pluginLoader,
    IEnumerable<FullReloadRequest> fullReloadRequests,
    IEnumerable<PartialReloadRequest> partialReloadRequests,
    IEnumerable<SoftReloadRequest> softReloadRequests
) : EventArgs
{
    internal IReloadRequestHandler RequestHandler = requestHandler;
    internal IPluginLoader PluginLoader = pluginLoader;
    internal IEnumerable<FullReloadRequest> FullReloadRequests = fullReloadRequests;
    internal IEnumerable<PartialReloadRequest> PartialReloadRequests = partialReloadRequests;
    internal IEnumerable<SoftReloadRequest> SoftReloadRequests = softReloadRequests;
}


internal class PartialReloadStartingEventArgs(
    IReloadRequestHandler requestHandler,
    IPluginLoader pluginLoader,
    IEnumerable<PartialReloadRequest> partialReloadRequests,
    ISet<string> allRequestedPluginGuids
) : EventArgs
{
    internal IReloadRequestHandler RequestHandler = requestHandler;
    internal IPluginLoader PluginLoader = pluginLoader;
    internal IEnumerable<PartialReloadRequest> PartialReloadRequests = partialReloadRequests;
    internal ISet<string> AllRequestedPluginGuids = allRequestedPluginGuids;
}

internal class SoftReloadStartingEventArgs(
    IReloadRequestHandler requestHandler,
    IPluginLoader pluginLoader,
    IEnumerable<SoftReloadRequest> softReloadRequests
) : EventArgs
{
    internal IReloadRequestHandler RequestHandler = requestHandler;
    internal IPluginLoader PluginLoader = pluginLoader;
    internal IEnumerable<SoftReloadRequest> SoftReloadRequests = softReloadRequests;
}
