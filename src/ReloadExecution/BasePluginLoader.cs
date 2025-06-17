using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BepInEx;

namespace Bloodpebble.ReloadExecution;


internal abstract class BasePluginLoader : ITriggersPluginLoaderEvents
{
    public event EventHandler<ReloadedPluginsEventArgs>? ReloadedPlugins;

    protected void OnReloadedPlugins(IList<PluginInfo> loadedPlugins, IEnumerable<string> unloadedPluginGuids)
    {
        ReloadedPlugins?.Invoke(this, new ReloadedPluginsEventArgs(loadedPlugins, unloadedPluginGuids));
    }

}
