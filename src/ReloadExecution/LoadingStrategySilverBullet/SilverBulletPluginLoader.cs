using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using ProjectM;

namespace Bloodpebble.ReloadExecution.LoadingStrategySilverBullet;

/// <summary>
///     TODO: write summary
/// </summary>
internal class SilverBulletPluginLoader : BasePluginLoader, IPluginLoader
{
    private Dictionary<string, PluginInfo> _reloadablePlugins = new();
    private ModifiedBepInExChainloader _bepinexChainloader = new();
    private PluginLoaderConfig _config;

    private DependencyGraph _dependencyGraph = new();

    public SilverBulletPluginLoader(PluginLoaderConfig config)
    {
        _config = config;
    }

    public IList<PluginInfo> ReloadAll()
    {
        UnloadAll();
        var loadedPlugins = LoadAll();
        OnReloadedAllPlugins(loadedPlugins);
        return loadedPlugins;
    }

    private IList<PluginInfo> LoadAll()
    {
        // first, make sure the bepinex chainloader knows about existing non-reloadable plugins that may be dependencies
        var normalPlugins = IL2CPPChainloader.Instance.Plugins;
        normalPlugins.ToList().ForEach(x => _bepinexChainloader.Plugins[x.Key] = x.Value);

        // load the additional plugins
        var loadedPlugins = _bepinexChainloader.LoadPlugins(_config.PluginsPath);
        foreach (var plugin in loadedPlugins)
        {
            _reloadablePlugins.Add(plugin.Metadata.GUID, plugin);

            var dependencyGuids = plugin.Dependencies.Select(d => d.DependencyGUID).ToHashSet();
            _dependencyGraph.AddVertex(plugin.Metadata.GUID, dependencyGuids);
        }
        BloodpebblePlugin.Logger.LogDebug($"Loaded plugins. resulting graph:\n{_dependencyGraph}");
        return loadedPlugins;
    }

    public void UnloadAll()
    {
        UnloadGiven(_reloadablePlugins.Keys);
    }

    public IList<PluginInfo> ReloadGiven(IEnumerable<string> pluginGUIDs)
    {
        UnloadGiven(pluginGUIDs);
        return LoadGiven(pluginGUIDs);
    }

    private void UnloadGiven(IEnumerable<string> pluginGUIDs)
    {
        BloodpebblePlugin.Logger.LogDebug($"requested to unload: {string.Join(", ", pluginGUIDs)}");
        var pluginGuidsToUnload = _dependencyGraph.FindAllVertexesToUnload(pluginGUIDs.ToHashSet());

        BloodpebblePlugin.Logger.LogDebug($"decided to unload: {string.Join(", ", pluginGuidsToUnload)}");
        //var pluginsToUnload = _reloadablePlugins.Values.Where(p => pluginGuidsToUnload.Contains(p.Metadata.GUID));

        List<BepInEx.PluginInfo> pluginsToUnload = new();
        foreach (var pluginGuid in pluginGuidsToUnload)
        {
            if (_bepinexChainloader.Plugins.TryGetValue(pluginGuid, out var plugin))
            {
                pluginsToUnload.Add(plugin);
            }            
        }
        
        BloodpebblePlugin.Logger.LogDebug($"4");
        //var pluginsToUnload = _bepinexChainloader.Plugins.Values.Where(p => pluginGuidsToUnload.Contains(p.Metadata.GUID));
        _bepinexChainloader.ModifyLoadOrder(pluginsToUnload);
        BloodpebblePlugin.Logger.LogDebug($"5");
        pluginsToUnload.Reverse();
        BloodpebblePlugin.Logger.LogDebug($"6");

        foreach (var plugin in pluginsToUnload)
        {
            _dependencyGraph.RemoveVertex(plugin.Metadata.GUID);
            _bepinexChainloader.UnloadPlugin(plugin);
            _reloadablePlugins.Remove(plugin.Metadata.GUID);
        }

        BloodpebblePlugin.Logger.LogDebug($"UnLoaded plugins {string.Join(", ", pluginGuidsToUnload)}. \nResulting graph:\n{_dependencyGraph}");
    }

    private IList<PluginInfo> LoadGiven(IEnumerable<string> pluginGUIDs)
    {
        // first, make sure the bepinex chainloader knows about existing non-reloadable plugins that may be dependencies
        var normalPlugins = IL2CPPChainloader.Instance.Plugins;
        normalPlugins.ToList().ForEach(x => _bepinexChainloader.Plugins[x.Key] = x.Value);

        // load the given plugins and any dependencies found
        var pluginsToLoad = DiscoverPluginsToLoad(pluginGUIDs);
        _bepinexChainloader.ModifyLoadOrder(pluginsToLoad);
        var loadedPlugins = _bepinexChainloader.LoadPlugins(pluginsToLoad);
        foreach (var plugin in loadedPlugins)
        {
            _reloadablePlugins.Add(plugin.Metadata.GUID, plugin);

            var dependencyGuids = plugin.Dependencies.Select(d => d.DependencyGUID).ToHashSet();
            _dependencyGraph.AddVertex(plugin.Metadata.GUID, dependencyGuids);
        }

        var pluginGuidsLoaded = loadedPlugins.Select(p => p.Metadata.GUID);
        BloodpebblePlugin.Logger.LogDebug($"Loaded plugins {string.Join(", ", pluginGuidsLoaded)}. \nResulting graph:\n{_dependencyGraph}");
        return loadedPlugins;
    }

    private IList<BepInEx.PluginInfo> DiscoverPluginsToLoad(IEnumerable<string> targetedPluginGUIDs)
    {
        var dependencyGraph = new DependencyGraph();

        var discoveredPlugins = _bepinexChainloader.DiscoverPluginsFrom(_config.PluginsPath);
        foreach (var plugin in discoveredPlugins)
        {
            var dependencyGuids = plugin.Dependencies.Select(d => d.DependencyGUID).ToHashSet();
            dependencyGraph.AddVertex(plugin.Metadata.GUID, dependencyGuids);
        }

        var pluginGuidsToLoad = dependencyGraph.FindAllVertexesToLoad(targetedPluginGUIDs.ToHashSet());
        return discoveredPlugins.Where(p => pluginGuidsToLoad.Contains(p.Metadata.GUID)).ToList();
    }

    // todo: can we get rid of this?
    public bool TryReloadPlugin(string guid, [NotNullWhen(true)] out PluginInfo? freshPlugin)
    {
        var loadedPlugins = ReloadAll();
        freshPlugin = loadedPlugins.FirstOrDefault(p => p?.Metadata.GUID == guid, null);
        return freshPlugin is not null;
    }

}
