
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

**Client Options:**
- The keybinding to reload is F6. Not currently configurable.

**Server Options:**
- `ReloadCommand` [default `!reload`]: Which text command (sent in chat) should be used to trigger reloading of plugins.\
User must first be AdminAuth'd (accomplished via console command).

### Support

Join the [modding community](https://vrisingmods.com/discord).

Post an issue on the [GitHub repository](https://github.com/cheesasaurus/Bloodpebble). 
