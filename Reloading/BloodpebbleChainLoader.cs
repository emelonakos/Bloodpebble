using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Unity.IL2CPP;
using ProjectM;

namespace Bloodpebble.Reloading;

class BloodpebbleChainloader
{
    private IList<PluginInfo> _plugins = new List<PluginInfo>();
    private ModifiedBepInExChainloader _bepinexChainloader = new ();

    public IList<PluginInfo> LoadPlugins(string pluginsPath)
    {
        // first, make sure the bepinex chainloader knows about existing non-reloadable plugins that may be dependencies
        var normalPlugins = IL2CPPChainloader.Instance.Plugins;
        normalPlugins.ToList().ForEach(x => _bepinexChainloader.Plugins[x.Key] = x.Value);

        // load the additional plugins
        var loadedPlugins = _bepinexChainloader.LoadPlugins(pluginsPath);
        _plugins = loadedPlugins;
        return loadedPlugins;
    }

    public void UnloadPlugins()
    {
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
    }
    
}
