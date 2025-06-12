namespace Bloodpebble.ReloadRequesting;

internal interface IReloadRequestHandler
{
    public void Update();
    public void HandleReloadRequested(FullReloadRequest request);
    public void HandleReloadRequested(PartialReloadRequest request);
    public void Dispose();
}
