
using Bloodpebble.Reloading;
using ScarletRCON.Shared;
using System.Diagnostics.CodeAnalysis;

namespace Bloodpebble.Features;

// todo: defer reloading to happen outside of the system updates.
public static class ReloadViaRCON
{
    private static IPluginLoader? _pluginLoader;

    internal static void Initialize(IPluginLoader pluginLoader)
    {
        _pluginLoader = pluginLoader;
        RconCommandRegistrar.RegisterAll();
    }

    internal static void Uninitialize()
    {
        RconCommandRegistrar.UnregisterAssembly();
    }

    [RconCommandCategory("Server Administration")]
    public static class RconCommands
    {
        [RconCommand("reloadplugins", "Reloads all plugins in the BloodpebblePlugins folder")]
        public static string ReloadAll()
        {
            ThrowIfPluginLoaderIsNull();
            BloodpebblePlugin.Logger.LogInfo("Reloading all plugins (triggered by RCON)...");
            _pluginLoader.ReloadAll();
            return "Reloaded all Bloodpebble plugins";
        }

        [RconCommand("reloadoneplugin", "Reloads a single plugin by its GUID", "reloadoneplugin <PluginGUID>")]
        public static string ReloadOne(string guid)
        {
            ThrowIfPluginLoaderIsNull();
            if (string.IsNullOrWhiteSpace(guid))
            {
                return "Error: Plugin GUID not provided.";
            }

            BloodpebblePlugin.Logger.LogInfo($"Reloading plugin {guid} (triggered by RCON)...");
            if (_pluginLoader.TryReloadPlugin(guid, out var reloadedPlugin))
            {
                return $"Reloaded plugin: {reloadedPlugin.Metadata.Name}";
            }
            else
            {
                return $"Failed to reload plugin with GUID: {guid}. See server console for details.";
            }
        }
    }

    [MemberNotNull(nameof(_pluginLoader))]
    private static void ThrowIfPluginLoaderIsNull()
    {
        if (_pluginLoader is null)
        {
            throw new System.Exception("_pluginLoader is null");
        }
    }

}