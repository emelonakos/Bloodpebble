using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Bloodpebble.ReloadExecution;
using Bloodpebble.ReloadRequesting;
using Il2CppSystem.Linq;


namespace Bloodpebble.ReloadRequestHandling;


/// <summary>
///    Captures ReloadRequests for later processing.
///    Does the processing when Update() is called.
/// </summary>
class DeferredReloadRequestHandler : BaseReloadRequestHandler
{
    private ManualLogSource _log;

    private ConcurrentQueue<FullReloadRequest> _fullReloadRequests = new();
    private ConcurrentQueue<PartialReloadRequest> _partialReloadRequests = new();
    private ConcurrentQueue<SoftReloadRequest> _softReloadRequests = new();
    private bool _shouldUpdate = false;

    public DeferredReloadRequestHandler(IPluginLoader pluginLoader, ManualLogSource log) : base(pluginLoader)
    {
        _log = log;
    }

    public override void HandleFullReloadRequested(FullReloadRequest request)
    {
        _fullReloadRequests.Enqueue(request);
        _shouldUpdate = true;
    }

    public override void HandlePartialReloadRequested(PartialReloadRequest request)
    {
        _partialReloadRequests.Enqueue(request);
        _shouldUpdate = true;
    }

    public override void HandleSoftReloadRequested(SoftReloadRequest request)
    {
        _softReloadRequests.Enqueue(request);
        _shouldUpdate = true;
    }

    public override void Update()
    {
        if (!_shouldUpdate)
        {
            return;
        }
        _shouldUpdate = false;

        // perform requested reloads

        var (fullReloadRequests, partialReloadRequests, softReloadRequests, allRequestedPluginGuids) = ExtractReloadRequests();

        IEnumerable<PluginInfo> pluginsReloaded;
        bool faulted = false;
        bool isFullReload = fullReloadRequests.Any();
        bool isPartialReload = !isFullReload && partialReloadRequests.Any();
        // todo: probably better to use a reload type enum
        try
        {
            if (isFullReload)
            {
                OnFullReloadStarting(fullReloadRequests, partialReloadRequests, softReloadRequests);
                pluginsReloaded = PluginLoader.ReloadAll();
            }
            else if (isPartialReload)
            {
                OnPartialReloadStarting(partialReloadRequests, allRequestedPluginGuids);
                pluginsReloaded = PluginLoader.ReloadGiven(allRequestedPluginGuids);
            }
            else
            {
                OnSoftReloadStarting(softReloadRequests);
                pluginsReloaded = PluginLoader.ReloadChanges();
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex);
            pluginsReloaded = new List<PluginInfo>();
            faulted = true;
        }

        // respond to requests

        foreach (var request in fullReloadRequests)
        {
            RespondToFullReloadRequest(request, pluginsReloaded, faulted);
        }

        var reloadedPluginGuids = pluginsReloaded.Select(pluginInfo => pluginInfo.Metadata.GUID).ToHashSet();
        foreach (var request in partialReloadRequests)
        {
            RespondToPartialReloadRequest(request, pluginsReloaded, reloadedPluginGuids, allRequestedPluginGuids, faulted, isFullReload);
        }

        foreach (var request in softReloadRequests)
        {
            RespondToSoftReloadRequest(request, pluginsReloaded, faulted, isFullReload, isPartialReload);
        }
    }

    private (
        List<FullReloadRequest> fullReloadRequests,
        List<PartialReloadRequest> partialReloadRequests,
        List<SoftReloadRequest> softReloadRequests,
        HashSet<string> allRequestedPluginGuids
    )
    ExtractReloadRequests()
    {
        List<FullReloadRequest> fullReloadRequests = new();
        List<PartialReloadRequest> partialReloadRequests = new();
        List<SoftReloadRequest> softReloadRequests = new();
        HashSet<string> allRequestedPluginGuids = new();

        while (_fullReloadRequests.TryDequeue(out var request))
        {
            fullReloadRequests.Add(request);
        }

        while (_partialReloadRequests.TryDequeue(out var request))
        {
            partialReloadRequests.Add(request);
            foreach (var pluginGuid in request.PluginGuidsToReload)
            {
                allRequestedPluginGuids.Add(pluginGuid);
            }
        }

        while (_softReloadRequests.TryDequeue(out var request))
        {
            softReloadRequests.Add(request);
        }

        return (fullReloadRequests, partialReloadRequests, softReloadRequests, allRequestedPluginGuids);
    }

    private void RespondToFullReloadRequest(FullReloadRequest request, IEnumerable<PluginInfo> pluginsReloaded, bool faulted)
    {
        try
        {
            request.Respond(new FullReloadResult(
                PluginsReloaded: pluginsReloaded,
                Status: faulted ? ReloadResultStatus.Faulted : ReloadResultStatus.Success
            ));
        }
        catch (Exception ex)
        {
            _log.LogError(ex);
        }
    }

    private void RespondToPartialReloadRequest(
        PartialReloadRequest request,
        IEnumerable<PluginInfo> pluginsReloaded,
        HashSet<string> reloadedPluginGuids,
        HashSet<string> allRequestedPluginGuids,
        bool faulted,
        bool wasFullReload
    )
    {
        try
        {
            var requestedPluginGuids = request.PluginGuidsToReload.ToHashSet();

            request.Respond(new PartialReloadResult(
                PluginsReloaded: pluginsReloaded,
                Status: PartialReloadResultStatus(faulted, requestedPluginGuids, reloadedPluginGuids),
                WasSuperseded: wasFullReload || requestedPluginGuids.IsProperSubsetOf(allRequestedPluginGuids)
            ));
        }
        catch (Exception ex)
        {
            _log.LogError(ex);
        }
    }

    private void RespondToSoftReloadRequest(SoftReloadRequest request, IEnumerable<PluginInfo> pluginsReloaded, bool faulted, bool wasFullReload, bool wasPartialReload)
    {
        // todo: partial vs full success
        try
        {
            request.Respond(new SoftReloadResult(
                PluginsReloaded: pluginsReloaded,
                Status: faulted ? ReloadResultStatus.Faulted : ReloadResultStatus.Success,
                WasSuperseded: wasFullReload || wasPartialReload
            ));
        }
        catch (Exception ex)
        {
            _log.LogError(ex);
        }
    }

}
