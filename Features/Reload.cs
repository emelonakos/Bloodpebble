using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Bloodpebble.Hooks;
using Bloodpebble.Reloading;
using Bloodpebble.Extensions;
using ScarletRCON.Shared;

namespace Bloodpebble.Features;

public static class Reload
{
#nullable disable
    private static string _reloadCommand;
    private static string _reloadPluginsFolder;
    private static float _autoReloadDelaySeconds;
    private static ReloadBehaviour _reloadBehavior;
    private static FileSystemWatcher _fileSystemWatcher;
#nullable enable

    private static KeyCode _keybinding = KeyCode.F6;
    private static bool _isPendingAutoReload = false;
    private static float autoReloadTimer;
    private static BloodpebbleChainloader _chainloader = new();

    internal static void Initialize(string reloadCommand, string reloadPluginsFolder, bool enableAutoReload, float autoReloadDelaySeconds)
    {
        _reloadCommand = reloadCommand;
        _reloadPluginsFolder = reloadPluginsFolder;
        _autoReloadDelaySeconds = autoReloadDelaySeconds;

        // note: no need to remove this on unload, since we'll unload the hook itself anyway
        Chat.OnChatMessage += HandleChatMessage;

        _reloadBehavior = BloodpebblePlugin.Instance.AddComponent<ReloadBehaviour>();

        LoadPlugins();

        if (enableAutoReload)
        {
            StartFileSystemWatcher();
        }
    }

    internal static void Uninitialize()
    {
        _fileSystemWatcher = null;
        Hooks.Chat.OnChatMessage -= HandleChatMessage;

        if (_reloadBehavior != null)
        {
            UnityEngine.Object.Destroy(_reloadBehavior);
        }
    }

    private static void HandleChatMessage(VChatEvent ev)
    {
        var parts = ev.Message.Split(' ');
        var command = parts[0];

        if (command != _reloadCommand && command != $"{_reloadCommand}one") return;
        if (!ev.User.IsAdmin) return; 

        ev.Cancel();

        if (command == _reloadCommand)
        {
            UnloadPlugins();
            var loadedPlugins = LoadPlugins();

            if (loadedPlugins.Count > 0)
            {
                var pluginNames = loadedPlugins.Select(plugin => plugin.Metadata.Name);
                ev.User.SendSystemMessage($"Reloaded {string.Join(", ", pluginNames)}. See console for details.");
            }
            else
            {
                ev.User.SendSystemMessage($"Did not reload any plugins because no reloadable plugins were found.");
            }
        }
        else if (command == $"{_reloadCommand}one")
        {
            if (parts.Length < 2)
            {
                ev.User.SendSystemMessage($"Usage: {_reloadCommand}one <PluginGUID>");
                return;
            }
            var guid = parts[1];
            var reloadedPlugin = _chainloader.ReloadPlugin(guid);
            if (reloadedPlugin != null)
            {
                ev.User.SendSystemMessage($"Reloaded plugin: {reloadedPlugin.Metadata.Name} ({reloadedPlugin.Metadata.GUID})");
            }
            else
            {
                ev.User.SendSystemMessage($"Failed to reload plugin with GUID: {guid}. Check console for details.");
            }
        }
    }

    private static IList<Bloodpebble.Reloading.PluginInfo> LoadPlugins()
    {
        if (!Directory.Exists(_reloadPluginsFolder))
        {
            Directory.CreateDirectory(_reloadPluginsFolder);
        }
        return _chainloader.LoadPlugins(_reloadPluginsFolder);
    }

    private static void UnloadPlugins()
    {
        _chainloader.UnloadPlugins();
    }

    private static void ReloadPlugins()
    {
        UnloadPlugins();
        var loadedPlugins = LoadPlugins();

        if (loadedPlugins.Count > 0)
        {
            var pluginNames = loadedPlugins.Select(plugin => plugin.Metadata.Name);
            BloodpebblePlugin.Logger.LogInfo($"Reloaded {string.Join(", ", pluginNames)}.");
        }
        else
        {
            BloodpebblePlugin.Logger.LogInfo($"Did not reload any plugins because no reloadable plugins were found.");
        }
    }

    private class ReloadBehaviour : UnityEngine.MonoBehaviour
    {
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(_keybinding))
            {
                BloodpebblePlugin.Logger.LogInfo("Reloading client plugins...");
                ReloadPlugins();
                _isPendingAutoReload = false;
            }
            else if (_isPendingAutoReload)
            {
                autoReloadTimer -= Time.unscaledDeltaTime;
                if (autoReloadTimer <= .0f)
                {
                    _isPendingAutoReload = false;
                    BloodpebblePlugin.Logger.LogInfo("Automatically reloading plugins...");
                    ReloadPlugins();
                }
            }
        }
    }

    private static void StartFileSystemWatcher()
    {
        _fileSystemWatcher = new FileSystemWatcher(_reloadPluginsFolder);
        _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
        _fileSystemWatcher.Filter = "*.dll";
        _fileSystemWatcher.Changed += FileChangedEventHandler;
        _fileSystemWatcher.Deleted += FileChangedEventHandler;
        _fileSystemWatcher.Created += FileChangedEventHandler;
        _fileSystemWatcher.Renamed += FileChangedEventHandler;
        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    private static void FileChangedEventHandler(object sender, FileSystemEventArgs args)
    {
        _isPendingAutoReload = true;
        autoReloadTimer = _autoReloadDelaySeconds;
    }

    [RconCommandCategory("Server Administration")]
    public static class RconCommands
    {
        [RconCommand("reloadplugins", "Reloads all plugins in the BloodpebblePlugins folder")]
        public static string ReloadAll()
        {
            BloodpebblePlugin.Logger.LogInfo("Reloading all plugins (triggered by RCON)...");
            ReloadPlugins();
            return "Reloaded all Bloodpebble plugins";
        }

        [RconCommand("reloadoneplugin", "Reloads a single plugin by its GUID", "reloadoneplugin <PluginGUID>")]
        public static string ReloadOne(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return "Error: Plugin GUID not provided.";
            }

            BloodpebblePlugin.Logger.LogInfo($"Reloading plugin {guid} (triggered by RCON)...");
            var reloadedPlugin = _chainloader.ReloadPlugin(guid);
            if (reloadedPlugin != null)
            {
                return $"Reloaded plugin: {reloadedPlugin.Metadata.Name}";
            }
            else
            {
                return $"Failed to reload plugin with GUID: {guid}. See server console for details.";
            }
        }
    }
}