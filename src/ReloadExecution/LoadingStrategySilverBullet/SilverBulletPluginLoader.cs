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
        var unloadedPluginGuids = UnloadAll();
        var loadedPlugins = LoadAll();
        OnReloadedPlugins(loadedPlugins, unloadedPluginGuids);
        return loadedPlugins;
    }

    public IEnumerable<string> UnloadAll()
    {
        var pluginGuidsToUnload = _reloadablePlugins.Keys;
        return UnloadGiven(pluginGuidsToUnload);;
    }

    private IList<PluginInfo> LoadAll()
    {
        PrepareForLoad();
        var loadedPlugins = _bepinexChainloader.LoadPlugins(_config.PluginsPath);
        ProcessFreshlyLoadedPlugins(loadedPlugins);

        var pluginGuidsLoaded = loadedPlugins.Select(p => p.Metadata.GUID);
        Log.LogDebug($"Loaded plugin(s): {string.Join(", ", pluginGuidsLoaded)}. \nResulting graph:\n{_dependencyGraph}");
        return loadedPlugins;
    }

    public IList<PluginInfo> ReloadGiven(IEnumerable<string> pluginGUIDs)
    {
        var unloadedPluginGuids = UnloadGivenAndDependents(pluginGUIDs);
        var loadedPlugins = LoadGivenAndDependencies(unloadedPluginGuids);
        OnReloadedPlugins(loadedPlugins, unloadedPluginGuids);
        return loadedPlugins;
    }

    private IEnumerable<string> UnloadGivenAndDependents(IEnumerable<string> pluginGUIDs)
    {
        var pluginGuidsToUnload = _dependencyGraph.FindAllVertexesToUnload(pluginGUIDs.ToHashSet());
        return UnloadGiven(pluginGuidsToUnload);
    }

    private IEnumerable<string> UnloadGiven(IEnumerable<string> pluginGUIDs)
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
            return [];
        }

        var unloadedPluginGuids = new List<string>();
        foreach (var plugin in pluginsToUnload)
        {
            var pluginGuid = plugin.Metadata.GUID;
            _dependencyGraph.RemoveVertex(pluginGuid);
            _bepinexChainloader.UnloadPlugin(plugin);
            var removed = _reloadablePlugins.Remove(pluginGuid);
            if (removed)
            {
                unloadedPluginGuids.Add(pluginGuid);
            }
        }
        Log.LogDebug($"Unloaded plugin(s): {string.Join(", ", pluginGUIDs)}. \nResulting graph:\n{_dependencyGraph}");
        return unloadedPluginGuids;
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

        var unloadedPluginGuids = UnloadGivenAndDependents(dirtyPluginGuids);

        var pluginsToLoad = _bepinexChainloader.DiscoverPluginsFrom(_config.PluginsPath)
            .Where(IsNotPluginLoaded);

        if (!pluginsToLoad.Any())
        {
            OnReloadedPlugins([], unloadedPluginGuids);
            return [];
        }

        var loadedPlugins = LoadGiven(pluginsToLoad);
        OnReloadedPlugins(loadedPlugins, unloadedPluginGuids);
        return loadedPlugins;
    }

}
