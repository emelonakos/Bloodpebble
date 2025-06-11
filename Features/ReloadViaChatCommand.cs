using System.Linq;
using Bloodpebble.Hooks;
using Bloodpebble.Reloading;
using Bloodpebble.Extensions;

namespace Bloodpebble.Features;


internal class ReloadViaChatCommand
{
    private IPluginLoader _pluginLoader;
    private string _reloadCommand;

    internal ReloadViaChatCommand(IPluginLoader pluginLoader, string reloadCommand)
    {
        _pluginLoader = pluginLoader;
        _reloadCommand = reloadCommand;
        Chat.OnChatMessage += HandleChatMessage;
    }

    public void Dispose()
    {
        Chat.OnChatMessage -= HandleChatMessage;
    }

    private void HandleChatMessage(VChatEvent ev)
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

    private void ChatCommandReloadAll(VChatEvent ev)
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

    private void ChatCommandReloadOne(VChatEvent ev, string[] msgParts)
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

}
