using System;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodpebble.Features;
using Bloodpebble.ReloadExecution;
using Bloodpebble.ReloadExecution.LoadingStategyBasic;
using Bloodpebble.ReloadExecution.LoadingStrategyIslands;
using Bloodpebble.ReloadExecution.LoadingStrategySilverBullet;
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
        private readonly EventLogger _eventLogger;

        private IReloadRequestHandler? _reloadRequestHandler;
        private ReloadViaChatCommand? _reloadViaChatCommand;
        private ReloadViaFileSystemChanges? _reloadViaFileSystemChanges;
        private ReloadViaKeyPress? _reloadViaKeyPress;
        private IPluginLoader? _pluginLoader;

        public BloodpebblePlugin() : base()
        {
            BloodpebblePlugin.Logger = Log;
            Instance = this;
            cfg = new BloodpebbleConfig(Config);
            _eventLogger = new EventLogger(Log);
        }

        public override void Load()
        {
            if (VWorld.IsServer)
            {
                Hooks.Chat.Initialize();
            }
            Hooks.GameFrame.Initialize();
            InitReloadFeatures();
            DoInitialPluginsLoad_After_BepInExLoadedOtherPlugins();
            Logger.LogInfo($"Bloodpebble v{MyPluginInfo.PLUGIN_VERSION} loaded.");
        }

        public override bool Unload()
        {
            _eventLogger.Unsubscribe();
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

        private void InitReloadFeatures()
        {
            Directory.CreateDirectory(cfg.PluginsFolder.Value);
            var loaderConfig = new PluginLoaderConfig(cfg.PluginsFolder.Value);
            IPluginLoader pluginLoader;
            switch (cfg.LoadingStrategy.Value.ToLowerInvariant())
            {
                case "islands":
                    pluginLoader = new IslandsPluginLoader(loaderConfig);
                    break;

                case "silverbullet":
                    pluginLoader = new SilverBulletPluginLoader(loaderConfig);
                    break;

                default: // default to basic
                case "basic":
                    pluginLoader = new BasicPluginLoader(loaderConfig);
                    break;
            }
            _eventLogger.Subscribe(pluginLoader);
            _pluginLoader = pluginLoader;

            // todo: config option to choose... maybe
            //_reloadRequestHandler = new ImmediateReloadRequestHandler(pluginLoader, Log);
            _reloadRequestHandler = new LateUpdateReloadRequestHandler(pluginLoader, Log);
            _eventLogger.Subscribe(_reloadRequestHandler);

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
        }

        private void DoInitialPluginsLoad()
        {
            if (_pluginLoader is null)
            {
                throw new Exception("_pluginLoader is null");
            }
            Log.LogInfo("Starting the Initial load of plugins.");
            _pluginLoader.ReloadAll();
        }

        private void DoInitialPluginsLoad_After_BepInExLoadedOtherPlugins()
        {
            // todo: see if there's something in bepinex that could be subscribed to,
            // instead of assuming that it will be finished before the next OnUpdate.
            // A harmony patch probably wouldn't work, because the target method would already be in the middle of execution. (loading bloodpebble)
            Hooks.GameFrame.OnUpdate += runOnce;
            void runOnce()
            {
                try
                {
                    DoInitialPluginsLoad();
                }
                finally
                {
                    Hooks.GameFrame.OnUpdate -= runOnce;
                }
            }
        }
        
    }
}