using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Bloodpebble.Reloading;

interface IPluginLoader
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

    // unload all loaded plugins

    /// <summary>
    ///     Unload all loaded plugins.
    /// </summary>
    public void UnloadAll();

}