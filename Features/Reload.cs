using System.IO;
using System.Linq;
using UnityEngine;
using Bloodpebble.Hooks;
using Bloodpebble.Reloading;
using Bloodpebble.Extensions;
using ScarletRCON.Shared;

namespace Bloodpebble.Features;


// todo: make this a "good" singleton with a static instance
//   currently has too many situations where this that or the other thing might not have been defined
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
    private static IPluginLoader _pluginLoader;

    internal static void Initialize(IPluginLoader pluginLoader, string reloadCommand, string reloadPluginsFolder, bool enableAutoReload, float autoReloadDelaySeconds)
    {
        _reloadCommand = reloadCommand;
        _reloadPluginsFolder = reloadPluginsFolder;
        _autoReloadDelaySeconds = autoReloadDelaySeconds;
        _pluginLoader = pluginLoader;

        // note: no need to remove this on unload, since we'll unload the hook itself anyway
        Chat.OnChatMessage += HandleChatMessage;

        _reloadBehavior = BloodpebblePlugin.Instance.AddComponent<ReloadBehaviour>();

        if (!Directory.Exists(_reloadPluginsFolder))
        {
            Directory.CreateDirectory(_reloadPluginsFolder);
        }

        _pluginLoader.ReloadAll();

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
        var msgParts = ev.Message.Split(' ');
        var command = msgParts[0];

        if (command != _reloadCommand && command != $"{_reloadCommand}one") return;
        if (!ev.User.IsAdmin) return;

        ev.Cancel();

        if (command == _reloadCommand)
        {
            ChatCommandReloadAll(ev);
        }
        else if (command == $"{_reloadCommand}one")
        {
            ChatCommandReloadOne(ev, msgParts);
        }
    }

    private static void ChatCommandReloadAll(VChatEvent ev)
    {
        var loadedPlugins = _pluginLoader.ReloadAll();

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

    private static void ChatCommandReloadOne(VChatEvent ev, string[] msgParts)
    {
        if (msgParts.Length < 2)
        {
            ev.User.SendSystemMessage($"Usage: {_reloadCommand}one <PluginGUID>");
            return;
        }

        var guid = msgParts[1];
        if (_pluginLoader.TryReloadPlugin(guid, out var reloadedPlugin))
        {
            ev.User.SendSystemMessage($"Reloaded plugin: {reloadedPlugin.Metadata.Name} ({reloadedPlugin.Metadata.GUID})");
        }
        else
        {
            ev.User.SendSystemMessage($"Failed to reload plugin with GUID: {guid}. Check console for details.");
        }
    }

    private class ReloadBehaviour : UnityEngine.MonoBehaviour
    {
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(_keybinding))
            {
                BloodpebblePlugin.Logger.LogInfo("Reloading client plugins...");
                _pluginLoader.ReloadAll();
                _isPendingAutoReload = false;
            }
            else if (_isPendingAutoReload)
            {
                autoReloadTimer -= Time.unscaledDeltaTime;
                if (autoReloadTimer <= .0f)
                {
                    _isPendingAutoReload = false;
                    BloodpebblePlugin.Logger.LogInfo("Automatically reloading plugins...");
                    _pluginLoader.ReloadAll();
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
            _pluginLoader.ReloadAll();
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

}