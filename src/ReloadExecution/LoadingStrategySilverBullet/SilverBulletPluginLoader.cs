using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace Bloodpebble.ReloadExecution.LoadingStrategySilverBullet;


/// <summary>
///     Each plugin has its own custom AssemblyLoadContext, with resolution to pre-loaded assemblies in other collectible contexts.
///     Reloading a plugin also reloads anything depending on it, but nothing else.
/// </summary>
internal class SilverBulletPluginLoader : BasePluginLoader, IPluginLoader
{
    private Dictionary<string, PluginInfo> _reloadablePlugins = new();
    private Dictionary<string, DateTime> _lastWriteTimes = new();
    private ModifiedBepInExChainloader _bepinexChainloader = new();
    private PluginLoaderConfig _config;
    protected ManualLogSource Log;

    private DependencyGraph _dependencyGraph = new();

    public SilverBulletPluginLoader(PluginLoaderConfig config, ManualLogSource log)
    {
        _config = config;
        Log = log;
    }

    public IList<PluginInfo> ReloadAll()
    {
        UnloadAll();
        var loadedPlugins = LoadAll();
        OnReloadedAllPlugins(loadedPlugins);
        return loadedPlugins;
    }

    public void UnloadAll()
    {
        UnloadGiven(_reloadablePlugins.Keys);
    }

    private IList<PluginInfo> LoadAll()
    {
        PrepareForLoad();
        var loadedPlugins = _bepinexChainloader.LoadPlugins(_config.PluginsPath);
        ProcessFreshlyLoadedPlugins(loadedPlugins);

        var pluginGuidsLoaded = loadedPlugins.Select(p => p.Metadata.GUID);
        BloodpebblePlugin.Logger.LogDebug($"Loaded plugin(s): {string.Join(", ", pluginGuidsLoaded)}. \nResulting graph:\n{_dependencyGraph}");
        return loadedPlugins;
    }

    public IList<PluginInfo> ReloadGiven(IEnumerable<string> pluginGUIDs)
    {
        var unloadedPluginGuids = UnloadGivenAndDependents(pluginGUIDs);
        return LoadGivenAndDependencies(unloadedPluginGuids);
    }

    private IEnumerable<string> UnloadGivenAndDependents(IEnumerable<string> pluginGUIDs)
    {
        var pluginGuidsToUnload = _dependencyGraph.FindAllVertexesToUnload(pluginGUIDs.ToHashSet());
        UnloadGiven(pluginGuidsToUnload);
        return pluginGuidsToUnload;
    }

    private void UnloadGiven(IEnumerable<string> pluginGUIDs)
    {
        var pluginsToUnload = _reloadablePlugins.Values.Where(p => pluginGUIDs.Contains(p.Metadata.GUID));
        _bepinexChainloader.ModifyUnloadOrder(pluginsToUnload);

        if (!pluginsToUnload.Any())
        {
            var sb = new StringBuilder().Append("Nothing to unload.");
            if (pluginGUIDs.Any())
            {
                sb.Append($" [{string.Join(", ", pluginGUIDs)}] requested, but not currently loaded.");
            }
            Log.LogDebug(sb.ToString());
            return;
        }

        foreach (var plugin in pluginsToUnload)
        {
            _dependencyGraph.RemoveVertex(plugin.Metadata.GUID);
            _bepinexChainloader.UnloadPlugin(plugin);
            _reloadablePlugins.Remove(plugin.Metadata.GUID);
        }
        Log.LogDebug($"Unloaded plugin(s): {string.Join(", ", pluginGUIDs)}. \nResulting graph:\n{_dependencyGraph}");
    }

    private IList<PluginInfo> LoadGivenAndDependencies(IEnumerable<string> pluginGUIDs)
    {
        var pluginsToLoad = DiscoverPluginsToLoad(pluginGUIDs);
        return LoadGiven(pluginsToLoad);
    }

    private IList<PluginInfo> LoadGiven(IEnumerable<PluginInfo> pluginsToLoad)
    {
        if (!pluginsToLoad.Any())
        {
            Log.LogDebug($"Did not find any plugins to load.");
            return [];
        }
        PrepareForLoad();
        _bepinexChainloader.ModifyLoadOrder(pluginsToLoad);
        var loadedPlugins = _bepinexChainloader.LoadPlugins(pluginsToLoad.ToList());
        ProcessFreshlyLoadedPlugins(loadedPlugins);

        var pluginGuidsLoaded = loadedPlugins.Select(p => p.Metadata.GUID);
        if (pluginGuidsLoaded.Any())
        {
            Log.LogDebug($"Loaded plugin(s): {string.Join(", ", pluginGuidsLoaded)}. \nResulting graph:\n{_dependencyGraph}");
        }
        else
        {
            Log.LogDebug($"Was not able to load any plugins.");
        }

        return loadedPlugins;
    }

    private void PrepareForLoad()
    {
        // make sure the bepinex chainloader knows about existing non-reloadable plugins that may be dependencies
        var normalPlugins = IL2CPPChainloader.Instance.Plugins;
        normalPlugins.ToList().ForEach(x => _bepinexChainloader.Plugins[x.Key] = x.Value);
    }

    private void ProcessFreshlyLoadedPlugins(IEnumerable<PluginInfo> loadedPlugins)
    {
        foreach (var plugin in loadedPlugins)
        {
            _reloadablePlugins.Add(plugin.Metadata.GUID, plugin);
            _lastWriteTimes[plugin.Metadata.GUID] = File.GetLastWriteTime(plugin.Location);

            var dependencyGuids = plugin.Dependencies.Select(d => d.DependencyGUID).ToHashSet();
            _dependencyGraph.AddVertex(plugin.Metadata.GUID, dependencyGuids);
        }
    }

    private IEnumerable<PluginInfo> DiscoverPluginsToLoad(IEnumerable<string> targetedPluginGUIDs)
    {
        var dependencyGraph = new DependencyGraph();

        var discoveredPlugins = _bepinexChainloader.DiscoverPluginsFrom(_config.PluginsPath);
        foreach (var plugin in discoveredPlugins)
        {
            var dependencyGuids = plugin.Dependencies.Select(d => d.DependencyGUID).ToHashSet();
            dependencyGraph.AddVertex(plugin.Metadata.GUID, dependencyGuids);
        }

        var pluginGuidsToLoad = dependencyGraph.FindAllVertexesToLoad(targetedPluginGUIDs.ToHashSet());

        return discoveredPlugins
            .Where(IsNotPluginLoaded)
            .Where(p => pluginGuidsToLoad.Contains(p.Metadata.GUID));
    }

    private bool IsPluginDirty(PluginInfo plugin)
    {
        if (!File.Exists(plugin.Location))
        {
            return true;
        }
        if (_lastWriteTimes.TryGetValue(plugin.Metadata.GUID, out var cachedLastWriteTime))
        {
            try
            {
                return cachedLastWriteTime < File.GetLastWriteTime(plugin.Location);
            }
            catch (Exception ex)
            {
                Log.LogWarning(ex);
            }
        }
        return true;
    }

    private bool IsNotPluginLoaded(PluginInfo plugin)
    {
        return !_reloadablePlugins.ContainsKey(plugin.Metadata.GUID);
    }

    public IList<PluginInfo> ReloadChanges()
    {
        var dirtyPluginGuids = _reloadablePlugins.Values
            .Where(IsPluginDirty)
            .Select(p => p.Metadata.GUID);

        UnloadGivenAndDependents(dirtyPluginGuids);

        var pluginsToLoad = _bepinexChainloader.DiscoverPluginsFrom(_config.PluginsPath)
            .Where(IsNotPluginLoaded);

        if (!pluginsToLoad.Any())
        {
            Log.LogDebug($"Did not find any plugin changes to load.");
            return [];
        }

        return LoadGiven(pluginsToLoad);
    }

}
