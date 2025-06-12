namespace Bloodpebble.ReloadRequesting;

internal interface IReloadRequestHandler : IReloadRequestSubscriber
{
    public void Dispose();
    public void Update();
    public void HandleFullReloadRequested(FullReloadRequest request);
    public void HandleFullReloadRequested(object? sender, FullReloadRequestedEventArgs ev);
    public void HandlePartialReloadRequested(PartialReloadRequest request);
    public void HandlePartialReloadRequested(object? sender, PartialReloadRequestedEventArgs ev);
}

internal interface IReloadRequestSubscriber
{
    public void Subscribe(IReloadRequestor requestor);
    public void Unsubscribe();
}
