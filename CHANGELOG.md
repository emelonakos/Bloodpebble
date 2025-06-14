# 1.3.1
- The initial load of plugins in the BloodpebblePlugins folder now happens AFTER normal BepInEx plugins loaded.
  - This should resolve some issues with dependencies not being found. Interestingly, bloodstone would have had the same problem.

# 1.3.0
- Plugin reloading now always happens during the LateUpdate phase of the [Unity event loop](https://docs.unity3d.com/Manual/execution-order.html).
  - previously varied depending on how the reload was triggered, which could cause issues when happening in the middle of Systems updates.
- Added `Islands` loading strategy, courtesy of [@Darreans](https://github.com/Darreans).
- Loading strategy can be chosen via config. Two options:
  - `Basic`: Robust, but slow if you have a lot of plugins and only want to reload one.
  - `Islands`: Fragile, but potentially faster when you have a lot of plugins and only want to reload one.
- Added `!reloadone <PluginGUID>` chat command for use with the `Islands` loading strategy.
- Added `bloodpebble.reloadplugin <PluginGUID>` RCON command for use with the `Islands` loading strategy.

# 1.2.1
- Added disclaimer section to README, explaining that not every plugin can be reloaded.
- Added brief documentation to README to help developers make their plugins reloadable.

# 1.2.0
- An RCON command `bloodpebble.reloadplugins` will be available if [ScarletRCON](https://thunderstore.io/c/v-rising/p/ScarletMods/ScarletRCON/) is installed.
- Bugfix: Plugins couldn't locate their reloadable dependencies.
- Bugfix: If an error occured while automatically unloading plugins, the autoloader got stuck infinitely trying to reload.

# 1.1.0
- Added optional capability to autoreload plugins when files changed. Enabled by default with a delay of 2 seconds.

# 1.0.1
- Bugfix: Resolves an issue where plugins ended up locked by the filesystem.

# 1.0.0
- Initial release