using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace Bloodpebble.ReloadExecution.LoadingStrategySilverBullet;

/// <summary>
///     Each plugin has its own custom AssemblyLoadContext, with resolution to pre-loaded assemblies in other collectible contexts.
///     Reloading a plugin also reloads anything depending on it, but nothing else.
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

        var pluginGuidsLoaded = loadedPlugins.Select(p => p.Metadata.GUID);
        BloodpebblePlugin.Logger.LogDebug($"Loaded plugin(s): {string.Join(", ", pluginGuidsLoaded)}. \nResulting graph:\n{_dependencyGraph}");
        return loadedPlugins;
    }

    public void UnloadAll()
    {
        UnloadGiven(_reloadablePlugins.Keys);
    }

    public IList<PluginInfo> ReloadGiven(IEnumerable<string> pluginGUIDs)
    {
        var pluginGuidsToUnload = _dependencyGraph.FindAllVertexesToUnload(pluginGUIDs.ToHashSet());
        UnloadGiven(pluginGuidsToUnload);
        return LoadGiven(pluginGuidsToUnload);
    }

    private void UnloadGiven(IEnumerable<string> pluginGUIDs)
    {
        var pluginsToUnload = _reloadablePlugins.Values.Where(p => pluginGUIDs.Contains(p.Metadata.GUID));
        _bepinexChainloader.ModifyLoadOrder(pluginsToUnload); // todo: ModifyLoadOrder does a bunch of checks besides sorting. shouldn't use that for unloading. or maybe not for unloading either
        pluginsToUnload.Reverse(); // we are unloading, so go in opposite order of loading

        if (!pluginsToUnload.Any())
        {
            var sb = new StringBuilder().Append("Nothing to unload.");
            if (pluginGUIDs.Any())
            {
                sb.Append($" [{string.Join(", ", pluginGUIDs)}] requested, but not currently loaded.");
            }
            BloodpebblePlugin.Logger.LogDebug(sb.ToString());
            return;
        }

        foreach (var plugin in pluginsToUnload)
        {
            _dependencyGraph.RemoveVertex(plugin.Metadata.GUID);
            _bepinexChainloader.UnloadPlugin(plugin);
            _reloadablePlugins.Remove(plugin.Metadata.GUID);
        }
        BloodpebblePlugin.Logger.LogDebug($"Unloaded plugin(s): {string.Join(", ", pluginGUIDs)}. \nResulting graph:\n{_dependencyGraph}");
    }

    private IList<PluginInfo> LoadGiven(IEnumerable<string> pluginGUIDs)
    {
        // first, make sure the bepinex chainloader knows about existing non-reloadable plugins that may be dependencies
        var normalPlugins = IL2CPPChainloader.Instance.Plugins;
        normalPlugins.ToList().ForEach(x => _bepinexChainloader.Plugins[x.Key] = x.Value);

        // load the given plugins and any dependencies found
        var pluginsToLoad = DiscoverPluginsToLoad(pluginGUIDs);
        if (!pluginsToLoad.Any())
        {
            BloodpebblePlugin.Logger.LogDebug($"Did not find any plugins to load.");
            return [];
        }

        _bepinexChainloader.ModifyLoadOrder(pluginsToLoad);
        var loadedPlugins = _bepinexChainloader.LoadPlugins(pluginsToLoad);
        foreach (var plugin in loadedPlugins)
        {
            _reloadablePlugins.Add(plugin.Metadata.GUID, plugin);

            var dependencyGuids = plugin.Dependencies.Select(d => d.DependencyGUID).ToHashSet();
            _dependencyGraph.AddVertex(plugin.Metadata.GUID, dependencyGuids);
        }

        var pluginGuidsLoaded = loadedPlugins.Select(p => p.Metadata.GUID);
        BloodpebblePlugin.Logger.LogDebug($"Loaded plugin(s): {string.Join(", ", pluginGuidsLoaded)}. \nResulting graph:\n{_dependencyGraph}");
        return loadedPlugins;
    }

    private IList<PluginInfo> DiscoverPluginsToLoad(IEnumerable<string> targetedPluginGUIDs)
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

    public IList<PluginInfo> ReloadChanges()
    {
        // todo: implement
        UnloadAll();
        var loadedPlugins = LoadAll();
        // todo: trigger
        return loadedPlugins;
    }
    
}
