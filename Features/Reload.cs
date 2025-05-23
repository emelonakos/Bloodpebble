using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
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
    private static ReloadBehaviour _clientBehavior;
#nullable enable

    private static KeyCode _keybinding = KeyCode.F6;
    private static BloodpebbleChainloader chainloader = new();

    internal static void Initialize(string reloadCommand, string reloadPluginsFolder)
    {
        _reloadCommand = reloadCommand;
        _reloadPluginsFolder = reloadPluginsFolder;

        // note: no need to remove this on unload, since we'll unload the hook itself anyway
        Chat.OnChatMessage += HandleReloadCommand;

        if (VWorld.IsClient)
        {
            _clientBehavior = BloodpebblePlugin.Instance.AddComponent<ReloadBehaviour>();
        }

        LoadPlugins();
    }

    internal static void Uninitialize()
    {
        Hooks.Chat.OnChatMessage -= HandleReloadCommand;

        if (_clientBehavior != null)
        {
            UnityEngine.Object.Destroy(_clientBehavior);
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

    private static IList<PluginInfo> LoadPlugins()
    {
        if (!Directory.Exists(_reloadPluginsFolder))
        {
            Directory.CreateDirectory(_reloadPluginsFolder);
        }
        return chainloader.LoadPlugins(_reloadPluginsFolder);
    }

    private static void UnloadPlugins()
    {
        chainloader.UnloadPlugins();
    }

    private class ReloadBehaviour : UnityEngine.MonoBehaviour
    {
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(_keybinding))
            {
                BloodpebblePlugin.Logger.LogInfo("Reloading client plugins...");
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
        }
    }
    
}