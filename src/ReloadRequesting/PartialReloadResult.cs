using System.Collections.Generic;
using Bloodpebble.Reloading;

namespace Bloodpebble.ReloadRequesting;

internal record PartialReloadResult(
    IEnumerable<PluginInfo> PluginsReloaded,
    ReloadResultStatus Status,
    bool WasSuperseded
);
