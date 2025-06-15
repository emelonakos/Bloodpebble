using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx.Unity.IL2CPP;
using ProjectM;

namespace Bloodpebble.ReloadExecution.LoadingStrategySilverBullet;

/// <summary>
///     TODO: write summary
/// </summary>
internal class SilverBulletPluginLoader : BasePluginLoader, IPluginLoader
{
    private IList<PluginInfo> _plugins = new List<PluginInfo>();
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

        // first, make sure the bepinex chainloader knows about existing non-reloadable plugins that may be dependencies
        var normalPlugins = IL2CPPChainloader.Instance.Plugins;
        normalPlugins.ToList().ForEach(x => _bepinexChainloader.Plugins[x.Key] = x.Value);

        // load the additional plugins
        var loadedPlugins = _bepinexChainloader.LoadPlugins(_config.PluginsPath);
        _plugins = loadedPlugins;

        foreach (var plugin in _plugins)
        {
            var dependencyGuids = plugin.Dependencies.Select(d => d.DependencyGUID).ToHashSet();
            _dependencyGraph.AddVertex(plugin.Metadata.GUID, dependencyGuids);
        }
        BloodpebblePlugin.Logger.LogInfo($"Loaded plugins. resulting graph:\n{_dependencyGraph}");

        OnReloadedAllPlugins(loadedPlugins);
        return loadedPlugins;
    }

    public IList<PluginInfo> ReloadGiven(IEnumerable<string> pluginGUIDs)
    {
        return ReloadAll();
    }

    public bool TryReloadPlugin(string guid, [NotNullWhen(true)] out PluginInfo? freshPlugin)
    {
        var loadedPlugins = ReloadAll();
        freshPlugin = loadedPlugins.FirstOrDefault(p => p?.Metadata.GUID == guid, null);
        return freshPlugin is not null;
    }

    public void UnloadAll()
    {
        for (int i = _plugins.Count - 1; i >= 0; i--)
        {
            var plugin = _plugins[i];
            _dependencyGraph.RemoveVertex(plugin.Metadata.GUID);
            _bepinexChainloader.UnloadPlugin(plugin);
            _plugins.RemoveAt(i);
        }
        BloodpebblePlugin.Logger.LogInfo($"UnLoaded plugins. resulting graph:\n{_dependencyGraph}");
    }

}
