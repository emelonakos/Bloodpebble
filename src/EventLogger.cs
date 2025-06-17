using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using Bloodpebble.ReloadExecution;
using Bloodpebble.ReloadRequesting;

namespace Bloodpebble;

internal class EventLogger(ManualLogSource Log)
{
    private ManualLogSource Log { get; } = Log;
    private List<IReloadRequestHandler> _requestHandlerSubscriptions = [];
    private List<IPluginLoader> _pluginLoaderSubscriptions = [];

    public void Subscribe(IReloadRequestHandler requestHandler)
    {
        _requestHandlerSubscriptions.Add(requestHandler);
        requestHandler.FullReloadStarting += HandleFullReloadStarting;
        requestHandler.PartialReloadStarting += HandlePartialReloadStarting;
        requestHandler.SoftReloadStarting += HandleSoftReloadStarting;
    }

    public void Subscribe(IPluginLoader pluginLoader)
    {
        _pluginLoaderSubscriptions.Add(pluginLoader);
        pluginLoader.ReloadedPlugins += HandleReloadedPlugins;
    }

    public void Unsubscribe()
    {
        foreach (var requestHandler in _requestHandlerSubscriptions)
        {
            requestHandler.FullReloadStarting -= HandleFullReloadStarting;
            requestHandler.PartialReloadStarting -= HandlePartialReloadStarting;
            requestHandler.SoftReloadStarting -= HandleSoftReloadStarting;
        }
        _requestHandlerSubscriptions.Clear();

        foreach (var pluginLoader in _pluginLoaderSubscriptions)
        {
            pluginLoader.ReloadedPlugins -= HandleReloadedPlugins;
        }
        _pluginLoaderSubscriptions.Clear();
    }

    private void HandleFullReloadStarting(object? sender, FullReloadStartingEventArgs ev)
    {
        var requestedByNames = ev.FullReloadRequests.Select(r => r.Requestor.GetType().Name);
        var partialRequestorNames = ev.PartialReloadRequests.Select(r => r.Requestor.GetType().Name);
        var softRequestorNames = ev.SoftReloadRequests.Select(r => r.Requestor.GetType().Name);

        var sb = new StringBuilder()
            .AppendLine("Starting a Full reload of plugins.")
            .AppendLine($"  Requestor(s): {string.Join(", ", requestedByNames)}")
            .AppendLine($"  Handler: {ev.RequestHandler.GetType().Name}")
            .Append($"  Strategy: {ev.PluginLoader.GetType().Name}");


        if (partialRequestorNames.Any() || softRequestorNames.Any())
        {
            sb.AppendLine();
            sb.AppendLine($"  Supercedes other reload requests.");
            if (partialRequestorNames.Any())
            {
                sb.Append($"    Partial reload request(s) from: {string.Join(", ", partialRequestorNames)}");
            }
            if (softRequestorNames.Any())
            {
                sb.Append($"    Soft reload request(s) from: {string.Join(", ", softRequestorNames)}");
            }
        }        

        Log.LogInfo(sb.ToString());
    }

    private void HandlePartialReloadStarting(object? sender, PartialReloadStartingEventArgs ev)
    {
        var requestedByNames = ev.PartialReloadRequests.Select(r => r.Requestor.GetType().Name);

        var sb = new StringBuilder()
            .AppendLine("Starting a Partial reload of plugins.")
            .AppendLine($"  Requestor(s): {string.Join(", ", requestedByNames)}")
            .AppendLine($"  Handler: {ev.RequestHandler.GetType().Name}")
            .AppendLine($"  Strategy: {ev.PluginLoader.GetType().Name}")
            .Append($"  Requested plugin GUIDs: {string.Join(", ", ev.AllRequestedPluginGuids)}");

        Log.LogInfo(sb.ToString());
    }

    private void HandleSoftReloadStarting(object? sender, SoftReloadStartingEventArgs ev)
    {
        var requestedByNames = ev.SoftReloadRequests.Select(r => r.Requestor.GetType().Name);

        var sb = new StringBuilder()
            .AppendLine("Starting a Soft reload of plugins.")
            .AppendLine($"  Requestor(s): {string.Join(", ", requestedByNames)}")
            .AppendLine($"  Handler: {ev.RequestHandler.GetType().Name}")
            .Append($"  Strategy: {ev.PluginLoader.GetType().Name}");

        Log.LogInfo(sb.ToString());
    }

    private void HandleReloadedPlugins(object? sender, ReloadedPluginsEventArgs e)
    {
        if (!e.UnloadedPluginGuids.Any() && !e.LoadedPlugins.Any())
        {
            Log.LogInfo($"Did not reload any plugins.");
            return;
        }

        var unloadedGuids = e.UnloadedPluginGuids.ToHashSet();
        var loadedGuids = e.LoadedPlugins.Select(p => p.Metadata.GUID).ToHashSet();

        var reloadedGuids = unloadedGuids.Intersect(loadedGuids);
        unloadedGuids = unloadedGuids.Except(reloadedGuids).ToHashSet();
        loadedGuids = loadedGuids.Except(reloadedGuids).ToHashSet();

        if (unloadedGuids.Any())
        {
            Log.LogInfo($"Unloaded {string.Join(", ", unloadedGuids)}.");
        }
        if (loadedGuids.Any())
        {
            Log.LogInfo($"Loaded {string.Join(", ", loadedGuids)}.");
        }
        if (reloadedGuids.Any())
        {
            Log.LogInfo($"Reloaded {string.Join(", ", reloadedGuids)}.");
        }
    }
    
}