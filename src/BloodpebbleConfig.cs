using System.Text;
using BepInEx.Configuration;

namespace Bloodpebble;

internal class BloodpebbleConfig
{
    internal ConfigEntry<string> ReloadCommand;
    internal ConfigEntry<string> PluginsFolder;
    internal ConfigEntry<bool> EnableAutoReload;
    internal ConfigEntry<float> AutoReloadDelaySeconds;
    internal ConfigEntry<string> LoadingStrategy;

    internal BloodpebbleConfig(ConfigFile configFile)
    {
        ReloadCommand = configFile.Bind("General", "ReloadCommand", "!reload", "Server chat command to reload plugins. User must first be AdminAuth'd (accomplished via console command).");
        PluginsFolder = configFile.Bind("General", "ReloadablePluginsFolder", "BepInEx/BloodpebblePlugins", "The folder to (re)load plugins from, relative to the game directory.");
        EnableAutoReload = configFile.Bind("AutoReload", "EnableAutoReload", true, new ConfigDescription("Automatically reloads all plugins if any of the files get changed (added/removed/modified)."));
        AutoReloadDelaySeconds = configFile.Bind("AutoReload", "AutoReloadDelaySeconds", 2.0f, new ConfigDescription("Delay in seconds before auto reloading."));
        LoadingStrategy = CreateConfigEntry_LoadingStrategy(configFile);
    }

    private ConfigEntry<string> CreateConfigEntry_LoadingStrategy(ConfigFile configFile)
    {
        string loaderDescription = new StringBuilder()
            .AppendLine("Which strategy to use for (re)loading plugins. Possible values:")
            .AppendLine()
            .AppendLine("Basic --------> Robust, but slow if you have a lot of plugins and only want to reload one.")
            .AppendLine("                All plugins share a loading context. Reloading one plugin reloads them all.")
            .AppendLine("                Handles plugin errors with troubleshooting messages;")
            .AppendLine("                attempts to recover and load every valid plugin.")
            .AppendLine()
            .AppendLine("Islands ------> Potentially faster when you have a lot of plugins and only want to reload one.")
            .AppendLine("                Plugins are partitioned into loading islands based on their dependencies.")
            .AppendLine("                Minimal handling of plugin errors. Either loads everything or nothing.")
            .AppendLine()
            .AppendLine("SilverBullet -> (Experimental) Robust and fast. Partial reloads only reload the bare minimum.")
            .AppendLine("                Each plugin is put into its own custom loading context and a context DAG is used for resolution.")
            .AppendLine("                Handles plugin errors with troubleshooting messages;")
            .AppendLine("                attempts to recover and load every valid plugin.")
            .ToString();

        return configFile.Bind(
            section: "Loader",
            key: "LoadingStrategy",
            defaultValue: "Basic",
            configDescription: new ConfigDescription(loaderDescription, new AcceptableValueList<string>("Basic", "Islands", "SilverBullet"))
        );
    }

}
