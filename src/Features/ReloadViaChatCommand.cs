using System.Linq;
using Bloodpebble.Hooks;
using Bloodpebble.Extensions;
using Bloodpebble.ReloadRequesting;

namespace Bloodpebble.Features;

// todo: defer reloading to happen outside of the system updates.
internal class ReloadViaChatCommand : BaseReloadRequestor
{
    private string _reloadCommand;

    internal ReloadViaChatCommand(string reloadCommand)
    {
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
            ChatCommandReloadAllAsync(ev);
        }
        else if (command == $"{_reloadCommand}one")
        {
            ChatCommandReloadOneAsync(ev, msgParts);
        }
    }

    private async void ChatCommandReloadAllAsync(VChatEvent ev)
    {
        var loadedPlugins = (await RequestFullReloadAsync()).PluginsReloaded;
        if (!loadedPlugins.Any())
        {
            ev.User.SendSystemMessage($"Did not reload any plugins because no reloadable plugins were found.");
        }
        else
        {
            var pluginNames = loadedPlugins.Select(plugin => plugin.Metadata.Name);
            ev.User.SendSystemMessage($"Reloaded {string.Join(", ", pluginNames)}. See console for details.");
        }
    }

    private async void ChatCommandReloadOneAsync(VChatEvent ev, string[] msgParts)
    {
        if (msgParts.Length < 2)
        {
            ev.User.SendSystemMessage($"Usage: {_reloadCommand}one <PluginGUID>");
            return;
        }

        var pluginGuid = msgParts[1];
        var reloadResult = await RequestPartialReloadAsync([pluginGuid]);
        var reloadedPlugin = reloadResult.PluginsReloaded.FirstOrDefault(p => p is not null && p.Metadata.GUID.Equals(pluginGuid), null);
        if (reloadedPlugin is null)
        {
            ev.User.SendSystemMessage($"Failed to reload plugin with GUID: {pluginGuid}. Check console for details.");
        }
        else
        {
            ev.User.SendSystemMessage($"Reloaded plugin: {reloadedPlugin.Metadata.Name} ({reloadedPlugin.Metadata.GUID})");
        }
    }

}
