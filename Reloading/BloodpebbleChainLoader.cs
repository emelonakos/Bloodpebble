using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace Bloodpebble.Reloading;

class BloodpebbleChainloader
{
    private IList<PluginInfo> _plugins = new List<PluginInfo>();
    private ChainloaderHelper chainloaderHelper = new ChainloaderHelper();

    public IList<PluginInfo> LoadPlugins(string pluginsPath)
    {
        // first, make sure the chainloaderHelper knows about existing non-reloadable plugins that may be dependencies
        var normalPlugins = IL2CPPChainloader.Instance.Plugins;
        normalPlugins.ToList().ForEach(x => chainloaderHelper.Plugins[x.Key] = x.Value);

        var discoveredPlugins = chainloaderHelper.DiscoverPluginsFrom(pluginsPath);
        var loadedPlugins = chainloaderHelper.LoadPlugins(discoveredPlugins);
        _plugins = loadedPlugins;
        return loadedPlugins;
    }

    public void UnloadPlugins()
    {
        for (int i = _plugins.Count - 1; i >= 0; i--)
        {
            var pluginGuid = _plugins[i].Metadata.GUID;
            var plugin = (BasePlugin)_plugins[i].Instance;

            if (!plugin.Unload())
            {
                Bloodpebble.BloodpebblePlugin.Logger.LogWarning($"Plugin {plugin.GetType().FullName} does not support unloading, skipping...");
            }
            else
            {
                chainloaderHelper.Plugins.Remove(pluginGuid);
                _plugins.RemoveAt(i);
            }
        }
    }
    
}
