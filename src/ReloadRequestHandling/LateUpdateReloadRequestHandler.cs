using BepInEx.Logging;
using Bloodpebble.Hooks;
using Bloodpebble.ReloadExecution;

namespace Bloodpebble.ReloadRequestHandling;


/// <summary>
///    Processes ReloadRequests during the LateUpdate phase of the Unity event loop.
///    https://docs.unity3d.com/Manual/execution-order.html
/// </summary>
internal class LateUpdateReloadRequestHandler : DeferredReloadRequestHandler
{
    public LateUpdateReloadRequestHandler(IPluginLoader pluginLoader, ManualLogSource log) : base(pluginLoader, log)
    {
        GameFrame.OnLateUpdate += Update;
    }

    public override void Dispose()
    {
        GameFrame.OnLateUpdate -= Update;
        base.Dispose();
    }

}