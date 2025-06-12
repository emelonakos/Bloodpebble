using System.Collections.Generic;
using Bloodpebble.Reloading;

namespace Bloodpebble.ReloadRequesting;

internal record FullReloadResult(
    IEnumerable<PluginInfo> PluginsReloaded,
    ReloadResultStatus Status
);
