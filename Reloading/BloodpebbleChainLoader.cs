using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using System.Reflection;
using System.Runtime.Loader;

namespace Bloodpebble.Reloading;

class BloodpebbleChainloader
{
    private readonly Dictionary<string, (PluginInfo Plugin, AssemblyLoadContext Context)> _plugins = new();
    private readonly ModifiedBepInExChainloader _bepinexChainloader = new();

    public IList<PluginInfo> LoadPlugins(string pluginsPath)
    {
        var normalPlugins = IL2CPPChainloader.Instance.Plugins;
        normalPlugins.ToList().ForEach(x => _bepinexChainloader.Plugins[x.Key] = x.Value);

        var sortedPlugins = _bepinexChainloader.DiscoverAndSortPlugins(pluginsPath);

        var newlyLoaded = new List<PluginInfo>();
        foreach (var pluginInfo in sortedPlugins)
        {
            try
            {
                var context = new AssemblyLoadContext(pluginInfo.Metadata.GUID, isCollectible: true);
                var loadedPlugin = _bepinexChainloader.LoadPlugin(pluginInfo, context, out _);
                _plugins[loadedPlugin.Metadata.GUID] = (loadedPlugin, context);
                newlyLoaded.Add(loadedPlugin);
            }
            catch (Exception e)
            {
                BloodpebblePlugin.Logger.LogError($"Failed to load plugin {pluginInfo.Metadata.Name}: {e.Message}");
            }
        }

        return newlyLoaded;
    }

    public void UnloadPlugins()
    {
        var guids = _plugins.Keys.Reverse().ToList();
        foreach (var guid in guids)
        {
            UnloadPlugin(guid);
        }
        _bepinexChainloader.RemoveSearchDirectory(BloodpebblePlugin.Instance.ConfigReloadablePluginsFolder.Value);
    }

    public void UnloadPlugin(string guid)
    {
        if (!_plugins.TryGetValue(guid, out var value)) return;

        var (pluginInfo, context) = value;
        var plugin = (BasePlugin)pluginInfo.Instance;
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
        _plugins.Remove(guid);

        context.Unload();
    }

    public PluginInfo? ReloadPlugin(string guid)
    {
        if (!_plugins.TryGetValue(guid, out var value))
        {
            BloodpebblePlugin.Logger.LogError($"Cannot reload plugin with GUID '{guid}' because it is not loaded.");
            return null;
        }

        var pluginPath = value.Plugin.Location;
        BloodpebblePlugin.Logger.LogInfo($"Attempting to reload plugin: {guid}");

        UnloadPlugin(guid);

        var pluginDirectory = Path.GetDirectoryName(pluginPath);
        if (pluginDirectory == null)
        {
            BloodpebblePlugin.Logger.LogError($"Could not determine directory for plugin GUID '{guid}'.");
            return null;
        }

        var sortedPlugins = _bepinexChainloader.DiscoverAndSortPlugins(pluginDirectory);
        var pluginToLoad = sortedPlugins.FirstOrDefault(p => p.Metadata.GUID == guid);

        if (pluginToLoad == null)
        {
            BloodpebblePlugin.Logger.LogError($"Could not find plugin file for GUID '{guid}'. A dependency may be missing.");
            return null;
        }

        try
        {
            var context = new AssemblyLoadContext(guid, isCollectible: true);
            var loadedPlugin = _bepinexChainloader.LoadPlugin(pluginToLoad, context, out _);
            _plugins[guid] = (loadedPlugin, context);
            BloodpebblePlugin.Logger.LogInfo($"Successfully reloaded {loadedPlugin.Metadata.Name}.");
            return loadedPlugin;
        }
        catch (Exception e)
        {
            BloodpebblePlugin.Logger.LogError($"Failed to reload plugin {guid}: {e.Message}");
            return null;
        }
    }
}