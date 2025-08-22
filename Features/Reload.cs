using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Bloodpebble.Hooks;
using Bloodpebble.Reloading;
using Bloodpebble.Extensions;
using ProjectM;
using ProjectM.Network;
using Steamworks;
using Bloodpebble.API;
using Unity.Collections;
using System.Text;

namespace Bloodpebble.Features;

public static class Reload
{
#nullable disable
    private static string _reloadCommand;
    private static string _reloadPluginsFolder;
    private static float _autoReloadDelaySeconds;
    private static FileSystemWatcher _fileSystemWatcher;
#nullable enable

    private static KeyCode _keybinding = KeyCode.F6;
    private static bool _isPendingAutoReload = false;
    private static float autoReloadTimer;
    private static BloodpebbleChainloader _chainloader = new();
    private static AdminAuthSystem? adminAuthSystem = null;

    internal static void Initialize(string reloadCommand, string reloadPluginsFolder, bool enableAutoReload, float autoReloadDelaySeconds)
    {
        _reloadCommand = reloadCommand;
        _reloadPluginsFolder = reloadPluginsFolder;
        _autoReloadDelaySeconds = autoReloadDelaySeconds;

        Chat.OnChatMessage += HandleChatMessage;

        LoadPlugins();
    }

    internal static void Uninitialize()
    {
        _fileSystemWatcher = null;
        Hooks.Chat.OnChatMessage -= HandleChatMessage;
    }

    private static void HandleChatMessage(VChatEvent ev)
    {
        if (adminAuthSystem == null)
        {
            adminAuthSystem = VWorld.Server.GetExistingSystemManaged<AdminAuthSystem>();
        }
        if (ev.Message.ToLower() != _reloadCommand) return;
        if (!ev.User.IsAdmin && !adminAuthSystem._LocalAdminList.Contains(ev.User.PlatformId)) return;

        ev.Cancel();

        var fixedStringMessage = new FixedString512Bytes("Reloading plugins -- you may experience a brief lag spike.".Warning());
        ServerChatUtils.SendSystemMessageToAllClients(
            VWorld.Server.EntityManager,
            ref fixedStringMessage
        );

        var action = () =>
        {
            UnloadPlugins();
            var loadedPlugins = LoadPlugins();

            if (loadedPlugins.Count > 0)
            {
                fixedStringMessage = new FixedString512Bytes("Plugins have been reloaded!".Warning());
                ServerChatUtils.SendSystemMessageToAllClients(
                    VWorld.Server.EntityManager,
                    ref fixedStringMessage
                );
            }
            else
            {
                ev.User.SendSystemMessage($"Did not reload any plugins because no reloadable plugins were found. Check the console for more details.");
            }
        };
        Chat.RunActionOnceAfterFrames(action, 2);
    }

    public static string Colorify(this string _string, Color _color)
    {
        StringBuilder m_stringBuilder = new StringBuilder();
        m_stringBuilder.Clear();
        m_stringBuilder.Append("<color=#");
        m_stringBuilder.Append(_color.ToHexString());
        m_stringBuilder.Append('>');
        m_stringBuilder.Append(_string);
        m_stringBuilder.Append("</color>");
        return m_stringBuilder.ToString();
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
}