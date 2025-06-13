using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Bloodpebble.Reloading;
using Bloodpebble.ReloadRequesting;


namespace Bloodpebble.ReloadRequestHandling;


/// <summary>
///     Immediately handles ReloadRequests when received
/// </summary>
class ImmediateReloadRequestHandler : BaseReloadRequestHandler
{
    private IPluginLoader _pluginLoader;
    private ManualLogSource _log;

    public ImmediateReloadRequestHandler(IPluginLoader pluginLoader,  ManualLogSource log)
    {
        _pluginLoader = pluginLoader;
        _log = log;
    }

    public override void HandleFullReloadRequested(FullReloadRequest request)
    {
        IEnumerable<PluginInfo> pluginsReloaded;
        bool faulted = false;
        try
        {
            pluginsReloaded = _pluginLoader.ReloadAll();
        }
        catch (Exception ex)
        {
            _log.LogError(ex);
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
        catch (Exception ex)
        {
            _log.LogError(ex);
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

    public override void Update()
    {
        // nothing to do; requests are immediately processed when received
    }
    
}
