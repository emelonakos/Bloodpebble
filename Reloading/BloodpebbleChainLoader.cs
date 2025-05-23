using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace Bloodpebble.Reloading;

class BloodpebbleChainloader
{
    private IList<PluginInfo> _plugins = new List<PluginInfo>();

    public IList<PluginInfo> LoadPlugins(string pluginsPath)
    {
        _plugins = IL2CPPChainloader.Instance.LoadPlugins(pluginsPath);
        return _plugins;
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
                IL2CPPChainloader.Instance.Plugins.Remove(pluginGuid);
                _plugins.RemoveAt(i);
            }
        }
    }
    
}
