using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Bloodpebble.Hooks;
using Bloodpebble.Reloading;
using Bloodpebble.Utils;
using Bloodpebble.Extensions;

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
        Chat.OnChatMessage += HandleReloadCommand;

        _reloadBehavior = BloodpebblePlugin.Instance.AddComponent<ReloadBehaviour>();

        LoadPlugins();

        if (enableAutoReload) {
            StartFileSystemWatcher();
        }
    }

    internal static void Uninitialize()
    {
        _fileSystemWatcher = null;
        Hooks.Chat.OnChatMessage -= HandleReloadCommand;

        if (_reloadBehavior != null)
        {
            UnityEngine.Object.Destroy(_reloadBehavior);
        }
    }

    private static void HandleReloadCommand(VChatEvent ev)
    {
        if (ev.Message != _reloadCommand) return;
        if (!ev.User.IsAdmin) return; // ignore non-admin reload attempts

        ev.Cancel();

        UnloadPlugins();
        var loadedPlugins = LoadPlugins();

        if (loadedPlugins.Count > 0)
        {
            var pluginNames = loadedPlugins.Select(plugin => plugin.Metadata.Name);
            ev.User.SendSystemMessage($"Reloaded {string.Join(", ", pluginNames)}. See console for details.");
        }
        else
        {
            ev.User.SendSystemMessage($"Did not reload any plugins because no reloadable plugins were found. Check the console for more details.");
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
                    BloodpebblePlugin.Logger.LogInfo("Automatically reloading plugins...");
                    ReloadPlugins();
                    _isPendingAutoReload = false;
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
    
}