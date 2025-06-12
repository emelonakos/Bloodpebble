using System;
using System.Collections.Generic;

namespace Bloodpebble.ReloadRequesting;

internal class BaseReleloadRequestor : IReloadRequestor
{
    public event EventHandler<FullReloadRequestedEventArgs>? FullReloadRequested;

    public event EventHandler<PartialReloadRequestedEventArgs>? PartialReloadRequested;

    protected void RequestFullReload(Action<FullReloadResult> responseHandler)
    {
        var request = new FullReloadRequest(responseHandler);
        FullReloadRequested?.Invoke(this, new FullReloadRequestedEventArgs(request));
    }

    protected void RequestPartialReload(IEnumerable<string> pluginGuidsToReload, Action<PartialReloadResult> responseHandler)
    {
        var request = new PartialReloadRequest(pluginGuidsToReload, responseHandler);
        PartialReloadRequested?.Invoke(this, new PartialReloadRequestedEventArgs(request));
    }

}
