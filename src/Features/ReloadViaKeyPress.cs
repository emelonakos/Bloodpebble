using Bloodpebble.Hooks;
using Bloodpebble.ReloadRequesting;
using UnityEngine;

namespace Bloodpebble.Features;

// todo: defer reloading to happen outside of the system updates.
internal class ReloadViaKeyPress : BaseReloadRequestor
{
    private KeyCode _reloadKeyCode;

    internal ReloadViaKeyPress(KeyCode reloadKeyCode = KeyCode.F6)
    {
        _reloadKeyCode = reloadKeyCode;
        GameFrame.OnLateUpdate += CheckKeypress;
    }

    internal void Dispose()
    {
        GameFrame.OnLateUpdate -= CheckKeypress;
    }

    private void CheckKeypress()
    {
        if (UnityEngine.Input.GetKeyDown(_reloadKeyCode))
        {
            BloodpebblePlugin.Logger.LogInfo("Reloading client plugins..."); // todo: move logging to request handler, not requester
            RequestFullReloadAsync();
        }
    }

}
