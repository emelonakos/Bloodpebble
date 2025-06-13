using System;

namespace Bloodpebble.ReloadRequesting;

internal record FullReloadRequest(
    Action<FullReloadResult> Respond
);
