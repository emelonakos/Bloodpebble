using System.Collections.Generic;
using System.Linq;
using Bloodpebble.Reloading;
using Bloodpebble.ReloadRequesting;


namespace Bloodpebble.ReloadRequestHandling;


/// <summary>
///     Immediately handles ReloadRequests when received
/// </summary>
class ImmediateReloadRequestHandler : BaseReloadRequestHandler
{
    private IPluginLoader _pluginLoader;

    public ImmediateReloadRequestHandler(IPluginLoader pluginLoader)
    {
        _pluginLoader = pluginLoader;
    }

    public override void HandleFullReloadRequested(FullReloadRequest request)
    {
        IEnumerable<PluginInfo> pluginsReloaded;
        bool faulted = false;
        try
        {
            pluginsReloaded = _pluginLoader.ReloadAll();
        }
        catch
        {
            pluginsReloaded = new List<PluginInfo>();
            faulted = true;
        }

        request.Respond(new FullReloadResult(
            PluginsReloaded: pluginsReloaded,
            Status: faulted ? ReloadResultStatus.Faulted : ReloadResultStatus.Success
        ));
    }

    public override void HandlePartialReloadRequested(PartialReloadRequest request)
    {
        IEnumerable<PluginInfo> pluginsReloaded;
        bool faulted = false;
        try
        {
            pluginsReloaded = _pluginLoader.ReloadGiven(request.PluginGuidsToReload);
        }
        catch
        {
            pluginsReloaded = new List<PluginInfo>();
            faulted = true;
        }

        var requestedGuids = request.PluginGuidsToReload.ToHashSet();
        var reloadedGuids = pluginsReloaded.Select(pluginInfo => pluginInfo.Metadata.GUID).ToHashSet();

        request.Respond(new PartialReloadResult(
            PluginsReloaded: pluginsReloaded,
            Status: PartialReloadResultStatus(faulted, requestedGuids, reloadedGuids),
            WasSuperseded: false
        ));
    }

    internal static ReloadResultStatus PartialReloadResultStatus(bool faulted, ISet<string> requestedGuids, ISet<string> reloadedGuids)
    {
        if (faulted)
        {
            return ReloadResultStatus.Faulted;
        }
        else if (!requestedGuids.IsSubsetOf(reloadedGuids))
        {
            return ReloadResultStatus.PartialSuccess;
        }
        return ReloadResultStatus.Success;
    }

    public override void Update()
    {
        // nothing to do; requests are immediately processed when received
    }
    
}
