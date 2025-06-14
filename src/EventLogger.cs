using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using Bloodpebble.Reloading;
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
    }

    public void Subscribe(IPluginLoader pluginLoader)
    {
        _pluginLoaderSubscriptions.Add(pluginLoader);
        pluginLoader.ReloadedAllPlugins += HandleReloadedAllPlugins;
    }

    public void Unsubscribe()
    {
        foreach (var requestHandler in _requestHandlerSubscriptions)
        {
            requestHandler.FullReloadStarting -= HandleFullReloadStarting;
            requestHandler.PartialReloadStarting -= HandlePartialReloadStarting;
        }
        _requestHandlerSubscriptions.Clear();

        foreach (var pluginLoader in _pluginLoaderSubscriptions)
        {
            pluginLoader.ReloadedAllPlugins -= HandleReloadedAllPlugins;
        }
        _pluginLoaderSubscriptions.Clear();
    }

    private void HandleFullReloadStarting(object? sender, FullReloadStartingEventArgs ev)
    {
        var requestedByNames = ev.FullReloadRequests.Select(r => r.Requestor.GetType().Name);
        var partialRequestorNames = ev.PartialReloadRequests.Select(r => r.Requestor.GetType().Name);

        var sb = new StringBuilder()
            .AppendLine("Starting a Full reload of plugins.")
            .AppendLine($"  Requestor(s): {string.Join(", ", requestedByNames)}")
            .AppendLine($"  Handler: {ev.RequestHandler.GetType().Name}")
            .Append($"  Strategy: {ev.PluginLoader.GetType().Name}");


        if (partialRequestorNames.Any())
        {
            sb.AppendLine();
            sb.AppendLine($"  Supercedes other reload requests.");
            sb.Append($"    Partial reload request(s) from: {string.Join(", ", partialRequestorNames)}");
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

    private void HandleReloadedAllPlugins(object? sender, ReloadedAllPluginsEventArgs e)
    {
        if (e.LoadedPlugins.Count > 0)
        {
            var pluginNames = e.LoadedPlugins.Select(plugin => plugin.Metadata.Name);
            Log.LogInfo($"Reloaded {string.Join(", ", pluginNames)}.");
        }
        else
        {
            Log.LogInfo($"Did not reload any plugins.");
        }
    }
    
}