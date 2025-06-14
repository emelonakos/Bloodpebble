
# Bloodpebble

![bloodpebble-banner](https://github.com/cheesasaurus/Bloodpebble/raw/main/bloodpebble-banner.png)

Bloodpebble is a lightweight alternative to [Bloodstone](https://github.com/decaprime/Bloodstone). It allows reloading plugins without restarting the game.

Differences from Bloodstone:
- Bloodpebble is only responsible for hot-reloading.
  - Bloodstone provides functionality for other things. ergo Bloodpebble has a lower maintenance cost when VRising updates.
- Bloodpebble checks plugin dependencies to load them in the correct order. 
  - Bloodstone does not. Working on a library for other plugins to use? Choose Bloodpebble for your hot-reloading needs.
- Bloodpebble is not required as a dependency. Simply drop your plugins into the BloodpebblePlugins folder and you're good to go.
  - Bloodstone can only reload plugins if they opt-in using its API. Broken bloodstone = broken dependent plugins.
- Bloodpebble can automatically reload plugins when files are changed in the BloodpebblePlugins folder.
  - Bloodstone cannot. A manual reload is required.

### Installation

- Install [BepInEx](https://v-rising.thunderstore.io/package/BepInEx/BepInExPack_V_Rising/).
- Extract _Bloodpebble.dll_ into _`(VRising folder)/BepInEx/plugins`_.
- Optional: extract any reloadable additional plugins into _`(VRising folder)/BepInEx/BloodpebblePlugins`_.

### Configuration

Bloodpebble supports the following configuration settings, available in `BepInEx/config/Bloodpebble.cfg`.

**Client/Server Options:**
- `ReloadablePluginsFolder` [default `BepInEx/BloodpebblePlugins`]: The path to the directory where reloadable plugins should be searched. Relative to the game directory.
- `EnableAutoReload` [default `true`]: Automatically reloads all plugins if any of the files get changed (added/removed/modified).
- `AutoReloadDelaySeconds` [default: `2`]: Delay in seconds before auto reloading.
- `LoadingStrategy` [default: `Basic`]: The strategy to use for reloading plugins. Choose which tradeoffs to make.

**Client Options:**
- The keybinding to reload is F6. Not currently configurable.

**Server Options:**
- `ReloadCommand` [default `!reload`]: Which text command (sent in chat) should be used to trigger reloading of plugins.\
User must first be AdminAuth'd (accomplished via console command).

### RCON

If [ScarletRCON](https://thunderstore.io/c/v-rising/p/ScarletMods/ScarletRCON/) is installed, bloodpebble will provide an RCON command to reload.
- `bloodpebble.reloadplugins` : Reload all valid plugins.
- `bloodpebble.reloadplugin <PluginGUID>` : Reload one plugin. Other plugins (e.g. dependents) can also be reloaded.
  - Recommended to use with the `Islands` loading strategy. Currently no benefit otherwise.

### Disclaimer

Not every plugin is going to be reloadable. You will still have to put some things in the usual BepInEx Plugins folder.

Notes for plugin developers:
- The assembly must be collectible. [This imposes restrictions.](https://learn.microsoft.com/en-us/dotnet/fundamentals/reflection/collectible-assemblies#restrictions-on-collectible-assemblies)
- Your plugin should implement the [Unload](https://docs.bepinex.dev/master/api/BepInEx.Unity.IL2CPP.BasePlugin.html) method to release any resources, unregister hooks, etc.
  - In particular, make sure you cleanup [anything that would affect its lifetime](https://learn.microsoft.com/en-us/dotnet/fundamentals/reflection/collectible-assemblies#lifetime-of-collectible-assemblies).

### Support

Join the [modding community](https://vrisingmods.com/discord).

Post an issue on the [GitHub repository](https://github.com/cheesasaurus/Bloodpebble). 
