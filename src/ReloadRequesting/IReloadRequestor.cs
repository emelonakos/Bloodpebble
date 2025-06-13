using System;

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
