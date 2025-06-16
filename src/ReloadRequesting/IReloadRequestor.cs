using System;
using System.Collections.Generic;
using BepInEx;


namespace Bloodpebble.ReloadRequesting;

internal interface IReloadRequestor
{
    public event EventHandler<FullReloadRequestedEventArgs>? FullReloadRequested;
    public event EventHandler<PartialReloadRequestedEventArgs>? PartialReloadRequested;
    public event EventHandler<SoftReloadRequestedEventArgs>? SoftReloadRequested;
}

internal class PartialReloadRequestedEventArgs(PartialReloadRequest request) : EventArgs
{
    internal PartialReloadRequest Request = request;
}

internal class FullReloadRequestedEventArgs(FullReloadRequest request) : EventArgs
{
    internal FullReloadRequest Request = request;
}

internal class SoftReloadRequestedEventArgs(SoftReloadRequest request) : EventArgs
{
    internal SoftReloadRequest Request = request;
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


internal record SoftReloadRequest(
    IReloadRequestor Requestor,
    Action<SoftReloadResult> Respond
);

internal record SoftReloadResult(
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

