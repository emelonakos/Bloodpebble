using System;
using System.Collections.Generic;
using Bloodpebble.Reloading;

namespace Bloodpebble.ReloadRequesting;

internal interface IReloadRequestor
{
    public event EventHandler<FullReloadRequestedEventArgs>? FullReloadRequested;
    public event EventHandler<PartialReloadRequestedEventArgs>? PartialReloadRequested;
}

internal class PartialReloadRequestedEventArgs(PartialReloadRequest request) : EventArgs
{
    internal PartialReloadRequest Request = request;
}

internal class FullReloadRequestedEventArgs(FullReloadRequest request) : EventArgs
{
    internal FullReloadRequest Request = request;
}


internal record FullReloadRequest(
    IReloadRequestor Requestor,
    Action<FullReloadResult> Respond
);

internal record FullReloadResult(
    IEnumerable<PluginInfo> PluginsReloaded,
    ReloadResultStatus Status
);


internal record PartialReloadRequest(
    IReloadRequestor Requestor,
    Action<PartialReloadResult> Respond,
    IEnumerable<string> PluginGuidsToReload
);

internal record PartialReloadResult(
    IEnumerable<PluginInfo> PluginsReloaded,
    ReloadResultStatus Status,
    bool WasSuperseded
);


internal enum ReloadResultStatus
{
    Success,
    PartialSuccess,
    Faulted
}

