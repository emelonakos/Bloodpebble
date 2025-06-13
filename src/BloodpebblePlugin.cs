using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
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
        private ConfigEntry<string> _reloadCommand;
        private ConfigEntry<string> _pluginsFolder;
        private ConfigEntry<bool> _enableAutoReload;
        private ConfigEntry<float> _autoReloadDelaySeconds;
        private ConfigEntry<string> _loadingStrategy;

        private IReloadRequestHandler? _reloadRequestHandler;
        private ReloadViaChatCommand? _reloadViaChatCommand;
        private ReloadViaFileSystemChanges? _reloadViaFileSystemChanges;
        private ReloadViaKeyPress? _reloadViaKeyPress;

        public BloodpebblePlugin() : base()
        {
            BloodpebblePlugin.Logger = Log;
            Instance = this;
            InitConfig();
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

        [MemberNotNull(nameof(_reloadCommand))]
        [MemberNotNull(nameof(_pluginsFolder))]
        [MemberNotNull(nameof(_enableAutoReload))]
        [MemberNotNull(nameof(_autoReloadDelaySeconds))]
        [MemberNotNull(nameof(_loadingStrategy))]
        private void InitConfig()
        {
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

        private void InitReload()
        {
            Directory.CreateDirectory(_pluginsFolder.Value);
            var loaderConfig = new PluginLoaderConfig(_pluginsFolder.Value);
            IPluginLoader pluginLoader;
            switch (_loadingStrategy.Value.ToLowerInvariant())
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

            _reloadViaChatCommand = new ReloadViaChatCommand(_reloadCommand.Value);
            _reloadRequestHandler.Subscribe(_reloadViaChatCommand);

            if (_enableAutoReload.Value)
            {
                _reloadViaFileSystemChanges = new ReloadViaFileSystemChanges(_pluginsFolder.Value, _autoReloadDelaySeconds.Value);
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