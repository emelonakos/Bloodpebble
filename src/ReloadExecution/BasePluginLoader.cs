using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Bloodpebble.ReloadExecution;


internal abstract class BasePluginLoader : ITriggersPluginLoaderEvents
{
    public event EventHandler<ReloadedAllPluginsEventArgs>? ReloadedAllPlugins;

    protected void OnReloadedAllPlugins(IList<PluginInfo> loadedPlugins)
    {
        ReloadedAllPlugins?.Invoke(this, new ReloadedAllPluginsEventArgs(loadedPlugins));
    }

}
