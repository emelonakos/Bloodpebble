using System.Linq;
using Bloodpebble.Hooks;
using Bloodpebble.Extensions;
using Bloodpebble.ReloadRequesting;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Bloodpebble.Features;


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
            ChatCommandReloadAsync(ev, msgParts);
        }
        else if (command == $"{_reloadCommand}one")
        {
            ChatCommandReloadOneAsync(ev, msgParts);
        }
    }

    private async void ChatCommandReloadAsync(VChatEvent ev, string[] msgParts)
    {
        IEnumerable<string> loadedPluginNames;

        if (msgParts.Length >= 2 && msgParts[1].ToLowerInvariant().Equals("hard"))
        {
            loadedPluginNames = await ReloadHard();
        }
        else
        {
            loadedPluginNames = await ReloadSoft();
        }

        if (!loadedPluginNames.Any())
        {
            ev.User.SendSystemMessage($"Did not reload any plugins because no reloadable plugins were found.");
        }
        else
        {
            ev.User.SendSystemMessage($"Reloaded {string.Join(", ", loadedPluginNames)}. See console for details.");
        }
    }

    private async Task<IEnumerable<string>> ReloadSoft()
    {
        var result = await RequestSoftReloadAsync();
        return result.PluginsReloaded.Select(plugin => plugin.Metadata.Name);
    }

    private async Task<IEnumerable<string>> ReloadHard()
    {
        var result = await RequestFullReloadAsync();
        return result.PluginsReloaded.Select(plugin => plugin.Metadata.Name);
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
