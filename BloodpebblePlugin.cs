﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodpebble.Features;
using Bloodpebble.API;

namespace Bloodpebble
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class BloodpebblePlugin : BasePlugin
    {
#nullable disable
        public static ManualLogSource Logger { get; private set; }
        internal static BloodpebblePlugin Instance { get; private set; }
#nullable enable
        private ConfigEntry<string> _reloadCommand;
        private ConfigEntry<string> _reloadPluginsFolder;
        private ConfigEntry<bool> _enableAutoReload;
        private ConfigEntry<float> _autoReloadDelaySeconds;

        public BloodpebblePlugin() : base()
        {
            BloodpebblePlugin.Logger = Log;
            Instance = this;
            _reloadCommand = Config.Bind("General", "ReloadCommand", "!reload", "Server chat command to reload plugins. User must first be AdminAuth'd (accomplished via console command).");
            _reloadPluginsFolder = Config.Bind("General", "ReloadablePluginsFolder", "BepInEx/BloodpebblePlugins", "The folder to (re)load plugins from, relative to the game directory.");
            _enableAutoReload = Config.Bind("AutoReload", "EnableAutoReload", true, new ConfigDescription("Automatically reloads all plugins if any of the files get changed (added/removed/modified)."));
            _autoReloadDelaySeconds = Config.Bind("AutoReload", "AutoReloadDelaySeconds", 2.0f, new ConfigDescription("Delay in seconds before auto reloading."));
        }

        public override void Load()
        {
            // Hooks
            if (VWorld.IsServer)
            {
                Hooks.Chat.Initialize();
            }

            Hooks.OnInitialize.Initialize();

            Logger.LogInfo($"Bloodpebble v{MyPluginInfo.PLUGIN_VERSION} loaded.");
            Reload.Initialize(_reloadCommand.Value, _reloadPluginsFolder.Value, _enableAutoReload.Value, _autoReloadDelaySeconds.Value);
        }

        public override bool Unload()
        {
            // Hooks
            if (VWorld.IsServer)
            {
                Hooks.Chat.Uninitialize();
            }

            Hooks.OnInitialize.Uninitialize();

            return true;
        }
    }
}