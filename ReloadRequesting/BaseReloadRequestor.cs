using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bloodpebble.ReloadRequesting;

internal abstract class BaseReloadRequestor : IReloadRequestor
{
    public event EventHandler<FullReloadRequestedEventArgs>? FullReloadRequested;

    public event EventHandler<PartialReloadRequestedEventArgs>? PartialReloadRequested;

    protected Task<FullReloadResult> RequestFullReloadAsync()
    {
        var tcs = new TaskCompletionSource<FullReloadResult>();
        RequestFullReload(tcs.SetResult);
        return tcs.Task;
    }

    protected void RequestFullReload(Action<FullReloadResult> responseHandler)
    {
        var request = new FullReloadRequest(responseHandler);
        FullReloadRequested?.Invoke(this, new FullReloadRequestedEventArgs(request));
    }

    protected Task<PartialReloadResult> RequestPartialReloadAsync(IEnumerable<string> pluginGuidsToReload)
    {
        var tcs = new TaskCompletionSource<PartialReloadResult>();
        RequestPartialReload(pluginGuidsToReload, tcs.SetResult);
        return tcs.Task;
    }

    protected void RequestPartialReload(IEnumerable<string> pluginGuidsToReload, Action<PartialReloadResult> responseHandler)
    {
        var request = new PartialReloadRequest(pluginGuidsToReload, responseHandler);
        PartialReloadRequested?.Invoke(this, new PartialReloadRequestedEventArgs(request));
    }

}
