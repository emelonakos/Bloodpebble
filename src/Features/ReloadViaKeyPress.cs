using Bloodpebble.Hooks;
using Bloodpebble.ReloadRequesting;
using UnityEngine;

namespace Bloodpebble.Features;


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
            if (Input.GetKey(KeyCode.LeftControl))
            {
                RequestFullReloadAsync();
            }
            else
            {
                RequestSoftReloadAsync();
            }
        }
    }

}
