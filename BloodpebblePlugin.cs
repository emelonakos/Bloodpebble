using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodpebble.Features;
using Bloodpebble.Utils;
using ProjectM.UI;
using ScarletRCON.Shared;

namespace Bloodpebble
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("markvaaz.ScarletRCON", BepInDependency.DependencyFlags.SoftDependency)]
    public class BloodpebblePlugin : BasePlugin
    {
#nullable disable
        public static ManualLogSource Logger { get; private set; }
        internal static BloodpebblePlugin Instance { get; private set; }
#nullable enable
        private ConfigEntry<string> _reloadCommand;
        private ConfigEntry<string> _pluginsFolder; 
        private ConfigEntry<bool> _enableAutoReload;
        private ConfigEntry<float> _autoReloadDelaySeconds;
        private ConfigEntry<string> _loadingStrategy;

        public BloodpebblePlugin() : base()
        {
            BloodpebblePlugin.Logger = Log;
            Instance = this;
            _reloadCommand = Config.Bind("General", "ReloadCommand", "!reload", "Server chat command to reload plugins. User must first be AdminAuth'd (accomplished via console command).");
            _pluginsFolder = Config.Bind("General", "ReloadablePluginsFolder", "BepInEx/BloodpebblePlugins", "The folder to (re)load plugins from, relative to the game directory.");
            _enableAutoReload = Config.Bind("AutoReload", "EnableAutoReload", true, new ConfigDescription("Automatically reloads all plugins if any of the files get changed (added/removed/modified)."));
            _autoReloadDelaySeconds = Config.Bind("AutoReload", "AutoReloadDelaySeconds", 2.0f, new ConfigDescription("Delay in seconds before auto reloading."));

            string loaderDescription = new StringBuilder()
                .AppendLine("Which strategy to use for (re)loading plugins. Possible values:")
                .AppendLine()
                .AppendLine("Basic   - Robust, but slow if you have a lot of plugins and only want to reload one.")
                .AppendLine("          All plugins share a loading context. Reloading one plugin reloads them all.")
                .AppendLine("          Handles plugin errors with troubleshooting messages;")
                .AppendLine("          attempts to recover and load every valid plugin.")
                .AppendLine()
                .AppendLine("Islands - Potentially faster when you have a lot of plugins and only want to reload one.")
                .AppendLine("          Plugins are partitioned into loading islands based on their dependencies.")
                .AppendLine("          Minimal handling of plugin errors. Either loads everything or nothing.")
                .AppendLine()
                .ToString();
                
            _loadingStrategy = Config.Bind(
                section: "Loader",
                key: "LoadingStrategy",
                defaultValue: "Basic",
                configDescription: new ConfigDescription(loaderDescription, new AcceptableValueList<string>("Basic", "Islands"))
            );
        }

        public override void Load()
        {
            // Hooks
            if (VWorld.IsServer)
            {
                Hooks.Chat.Initialize();
            }

            Logger.LogInfo($"Bloodpebble v{MyPluginInfo.PLUGIN_VERSION} loaded.");
            Reload.Initialize(_reloadCommand.Value, _pluginsFolder.Value, _enableAutoReload.Value, _autoReloadDelaySeconds.Value, _loadingStrategy.Value);

            RconCommandRegistrar.RegisterAll();
        }

        public override bool Unload()
        {
            RconCommandRegistrar.UnregisterAssembly();

            // Hooks
            if (VWorld.IsServer)
            {
                Hooks.Chat.Uninitialize();
            }
            return true;
        }
    }
}