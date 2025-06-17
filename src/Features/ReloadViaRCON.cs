
using Bloodpebble.ReloadRequesting;
using ScarletRCON.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bloodpebble.Features;


internal class ReloadViaRCON : BaseReloadRequestor
{
    internal static ReloadViaRCON? Instance;

    internal static ReloadViaRCON Initialize()
    {
        Instance = new ReloadViaRCON();
        RconCommandRegistrar.RegisterAll();
        return Instance;
    }

    internal static void Uninitialize()
    {
        RconCommandRegistrar.UnregisterAssembly();
    }

    [RconCommandCategory("Server Administration")]
    public static class RconCommands
    {
        [RconCommand("reloadPluginsHard", "Reloads all plugins in the BloodpebblePlugins folder")]
        public async static Task<string> ReloadHardAsync()
        {
            return await ReloadHard();
        }

        [RconCommand("reloadPlugins", "Reloads changed plugins in the BloodpebblePlugins folder")]
        public async static Task<string> ReloadSoftAsync()
        {
            return await ReloadSoft();           
        }

        private static async Task<string> ReloadSoft()
        {
            if (Instance is null)
            {
                return "Error: missing ReloadViaRCON instance";
            }
            var result = await Instance.RequestSoftReloadAsync();
            var pluginNames = result.PluginsReloaded.Select(p => p.Metadata.Name);
            return DescribeResult(result.Status, pluginNames);
        }

        private static async Task<string> ReloadHard()
        {
            if (Instance is null)
            {
                return "Error: missing ReloadViaRCON instance";
            }
            var result = await Instance.RequestFullReloadAsync();
            var pluginNames = result.PluginsReloaded.Select(p => p.Metadata.Name);
            return DescribeResult(result.Status, pluginNames);
        }

        private static string DescribeResult(ReloadResultStatus status, IEnumerable<string> loadedPluginNames)
        {
            var joinedPluginNames = string.Join(", ", loadedPluginNames);
            switch (status)
            {
                case ReloadResultStatus.Success:
                    return loadedPluginNames.Any() ? $"Reloaded plugins: {joinedPluginNames}" : "Nothing to reload";
                case ReloadResultStatus.PartialSuccess:
                    return loadedPluginNames.Any() ? $"Reloaded only some plugins: {joinedPluginNames}" : "Could not reload anything";
                default: // default to Faulted
                case ReloadResultStatus.Faulted:
                    return "Error: An exception occurred while attempting to reload. Check logs for details.";
            }
        }

        [RconCommand("reloadplugin", "Reloads a single plugin by its GUID", "reloadplugin <PluginGUID>")]
        public async static Task<string> ReloadOneAsync(string guid)
        {
            if (Instance is null)
            {
                return "Error: missing ReloadViaRCON instance";
            }
            if (string.IsNullOrWhiteSpace(guid))
            {
                return "Error: Plugin GUID not provided.";
            }

            var result = await Instance.RequestPartialReloadAsync([guid]);
            var otherPluginNames = result.PluginsReloaded.Where(p => p.Metadata.GUID != guid).Select(p => p.Metadata.Name);

            switch (result.Status)
            {
                case ReloadResultStatus.Success:
                    if (otherPluginNames.Any())
                    {
                        return $"Reloaded plugin \"{guid}\", along with other plugins: {string.Join(", ", otherPluginNames)}";
                    }
                    return $"Reloaded plugin \"{guid}\".";

                case ReloadResultStatus.PartialSuccess:
                    if (otherPluginNames.Any())
                    {
                        return $"Failed to reload plugin \"{guid}\", but reloaded other plugins: {string.Join(", ", otherPluginNames)}";
                    }
                    return $"Failed to reload plugin \"{guid}\".";

                default:
                case ReloadResultStatus.Faulted:
                    return "Error: An exception occurred while attempting to reload. Check logs for details.";
            }

        }
    }

}
