using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodpebble.Features;
using Bloodpebble.Reloading;
using Bloodpebble.Reloading.LoaderBasic;
using Bloodpebble.Reloading.LoaderIslands;
using Bloodpebble.ReloadRequestHandling;
using Bloodpebble.ReloadRequesting;
using Bloodpebble.Utils;

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
        private readonly BloodpebbleConfig cfg;

        private IReloadRequestHandler? _reloadRequestHandler;
        private ReloadViaChatCommand? _reloadViaChatCommand;
        private ReloadViaFileSystemChanges? _reloadViaFileSystemChanges;
        private ReloadViaKeyPress? _reloadViaKeyPress;

        public BloodpebblePlugin() : base()
        {
            BloodpebblePlugin.Logger = Log;
            Instance = this;
            cfg = new BloodpebbleConfig(Config);
        }

        public override void Load()
        {
            if (VWorld.IsServer)
            {
                Hooks.Chat.Initialize();
            }
            Hooks.GameFrame.Initialize();
            InitReload();
            Logger.LogInfo($"Bloodpebble v{MyPluginInfo.PLUGIN_VERSION} loaded.");
        }

        public override bool Unload()
        {
            _reloadRequestHandler?.Dispose();
            ReloadViaRCON.Uninitialize();
            _reloadViaFileSystemChanges?.Dispose();
            _reloadViaChatCommand?.Dispose();
            _reloadViaKeyPress?.Dispose();
            Hooks.GameFrame.Initialize();
            if (VWorld.IsServer)
            {
                Hooks.Chat.Uninitialize();
            }
            return true;
        }

        private void InitReload()
        {
            Directory.CreateDirectory(cfg.PluginsFolder.Value);
            var loaderConfig = new PluginLoaderConfig(cfg.PluginsFolder.Value);
            IPluginLoader pluginLoader;
            switch (cfg.LoadingStrategy.Value.ToLowerInvariant())
            {
                case "islands":
                    pluginLoader = new IslandsPluginLoader(loaderConfig);
                    break;

                default: // default to basic
                case "basic":
                    pluginLoader = new BasicPluginLoader(loaderConfig);
                    break;
            }
            pluginLoader.ReloadedAllPlugins += HandleReloadedAllPlugins;

            _reloadRequestHandler = new ImmediateReloadRequestHandler(pluginLoader);

            _reloadViaChatCommand = new ReloadViaChatCommand(cfg.ReloadCommand.Value);
            _reloadRequestHandler.Subscribe(_reloadViaChatCommand);

            if (cfg.EnableAutoReload.Value)
            {
                _reloadViaFileSystemChanges = new ReloadViaFileSystemChanges(cfg.PluginsFolder.Value, cfg.AutoReloadDelaySeconds.Value);
                _reloadRequestHandler.Subscribe(_reloadViaFileSystemChanges);
            }

            if (VWorld.IsClient)
            {
                _reloadViaKeyPress = new ReloadViaKeyPress();
                _reloadRequestHandler.Subscribe(_reloadViaKeyPress);
            }

            var reloadViaRCON = ReloadViaRCON.Initialize();
            _reloadRequestHandler.Subscribe(reloadViaRCON);
            
            pluginLoader.ReloadAll();
        }

        private void HandleReloadedAllPlugins(object? sender, ReloadedAllPluginsEventArgs e)
        {
            if (e.LoadedPlugins.Count > 0)
            {
                var pluginNames = e.LoadedPlugins.Select(plugin => plugin.Metadata.Name);
                Log.LogInfo($"Reloaded {string.Join(", ", pluginNames)}.");
            }
            else
            {
                Log.LogInfo($"Did not reload any plugins.");
            }
        }
        
    }
}