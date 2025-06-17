using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace Bloodpebble.ReloadExecution.LoadingStategyBasic;

/// <summary>
///     Loads all plugins into a single AssemblyLoadContext.
///     Reloading a plugin reloads all plugins.
/// </summary>
[Obsolete("Will be removed once SilverBullet is stable")]
class BasicPluginLoader : BasePluginLoader, IPluginLoader
{
    private IList<PluginInfo> _plugins = new List<PluginInfo>();
    private ModifiedBepInExChainloader _bepinexChainloader = new();
    private PluginLoaderConfig _config;

    public BasicPluginLoader(PluginLoaderConfig config)
    {
        _config = config;
    }

    public IList<PluginInfo> ReloadAll()
    {
        var unloadedPluginGuids = UnloadAll();
        var loadedPlugins = LoadAll();
        OnReloadedPlugins(loadedPlugins, unloadedPluginGuids);
        return loadedPlugins;
    }

    public IList<PluginInfo> ReloadGiven(IEnumerable<string> pluginGUIDs)
    {
        var unloadedPluginGuids = UnloadAll();
        var loadedPlugins = LoadAll();
        OnReloadedPlugins(loadedPlugins, unloadedPluginGuids);
        return loadedPlugins;
    }

    public IEnumerable<string> UnloadAll()
    {
        var pluginGuids = _plugins.Select(p => p.Metadata.GUID).ToList();

        for (int i = _plugins.Count - 1; i >= 0; i--)
        {
            var pluginInfo = _plugins[i];
            var plugin = (BasePlugin)_plugins[i].Instance;
            var assemblyName = plugin.GetType().Assembly.GetName();
            var pluginName = $"{assemblyName.Name} {assemblyName.Version}";

            try
            {
                if (!plugin.Unload())
                {
                    BloodpebblePlugin.Logger.LogWarning($"Plugin {pluginName} might not be reloadable. (Plugin.Unload returned false)");
                }
            }
            catch (Exception ex)
            {
                BloodpebblePlugin.Logger.LogError($"Error unloading plugin {pluginName}:");
                BloodpebblePlugin.Logger.LogError(ex);
            }
            _bepinexChainloader.Plugins.Remove(pluginInfo.Metadata.GUID);
            _plugins.RemoveAt(i);
        }
        _bepinexChainloader.UnloadAssemblies();
        return pluginGuids;
    }

    private IList<PluginInfo> LoadAll()
    {
        // first, make sure the bepinex chainloader knows about existing non-reloadable plugins that may be dependencies
        var normalPlugins = IL2CPPChainloader.Instance.Plugins;
        normalPlugins.ToList().ForEach(x => _bepinexChainloader.Plugins[x.Key] = x.Value);

        // load the additional plugins
        var loadedPlugins = _bepinexChainloader.LoadPlugins(_config.PluginsPath);
        _plugins = loadedPlugins;
        return loadedPlugins;
    }

    public IList<PluginInfo> ReloadChanges()
    {
        var unloadedPluginGuids = UnloadAll();
        var loadedPlugins = LoadAll();
        OnReloadedPlugins(loadedPlugins, unloadedPluginGuids);
        return loadedPlugins;
    }

}
