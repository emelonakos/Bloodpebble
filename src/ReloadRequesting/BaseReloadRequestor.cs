using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bloodpebble.ReloadRequesting;

internal abstract class BaseReloadRequestor : IReloadRequestor
{
    public event EventHandler<FullReloadRequestedEventArgs>? FullReloadRequested;
    public event EventHandler<PartialReloadRequestedEventArgs>? PartialReloadRequested;
    public event EventHandler<SoftReloadRequestedEventArgs>? SoftReloadRequested;

    protected Task<FullReloadResult> RequestFullReloadAsync()
    {
        var tcs = new TaskCompletionSource<FullReloadResult>();
        RequestFullReload(tcs.SetResult);
        return tcs.Task;
    }

    protected void RequestFullReload(Action<FullReloadResult> responseHandler)
    {
        var requesterName = GetType().Name;
        var request = new FullReloadRequest(this, responseHandler);
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
        var requesterName = GetType().Name;
        var request = new PartialReloadRequest(this, responseHandler, pluginGuidsToReload);
        PartialReloadRequested?.Invoke(this, new PartialReloadRequestedEventArgs(request));
    }

    protected Task<SoftReloadResult> RequestSoftReloadAsync()
    {
        var tcs = new TaskCompletionSource<SoftReloadResult>();
        RequestSoftReload(tcs.SetResult);
        return tcs.Task;
    }

    protected void RequestSoftReload(Action<SoftReloadResult> responseHandler)
    {
        var requesterName = GetType().Name;
        var request = new SoftReloadRequest(this, responseHandler);
        SoftReloadRequested?.Invoke(this, new SoftReloadRequestedEventArgs(request));
    }

}
