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