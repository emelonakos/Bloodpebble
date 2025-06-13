using System;
using System.Collections.Generic;

namespace Bloodpebble.ReloadRequesting;

internal record PartialReloadRequest(
    IEnumerable<string> PluginGuidsToReload,
    Action<PartialReloadResult> Respond
);