using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Bloodpebble.Reloading;

interface IPluginLoader : ITriggersPluginLoaderEvents
{

    /// <summary>
    ///     (Re)load all discoverable plugins.
    /// </summary>
    public IList<PluginInfo> ReloadAll();

    /// <summary>
    ///     (Re)load the given plugins. Other plugins (e.g. dependents) can also be reloaded.
    /// </summary>
    public IList<PluginInfo> ReloadGiven(IEnumerable<string> pluginGUIDs);

    /// <summary>
    ///     (Re)load the given plugin. other plugins (e.g. dependents) can also be reloaded.
    /// </summary>
    public bool TryReloadPlugin(string guid, [NotNullWhen(true)] out PluginInfo? freshPlugin);

    /// <summary>
    ///     Unload all loaded plugins.
    /// </summary>
    public void UnloadAll();

}

interface ITriggersPluginLoaderEvents
{
    public event EventHandler<ReloadedAllPluginsEventArgs>? ReloadedAllPlugins;
}


internal class ReloadedAllPluginsEventArgs(IList<PluginInfo> LoadedPlugins) : EventArgs
{
    internal IList<PluginInfo> LoadedPlugins { get; } = LoadedPlugins;
}
