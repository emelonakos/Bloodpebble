using UnityEngine;
using Bloodpebble.Reloading;

namespace Bloodpebble.Features;


// todo: make this a "good" singleton with a static instance
//   currently has too many situations where this that or the other thing might not have been defined
public static class Reload
{
#nullable disable
    private static ReloadBehaviour _reloadBehavior;
#nullable enable

    private static KeyCode _keybinding = KeyCode.F6;
    private static IPluginLoader _pluginLoader;

    internal static void Initialize(IPluginLoader pluginLoader)
    {
        _pluginLoader = pluginLoader;

        _reloadBehavior = BloodpebblePlugin.Instance.AddComponent<ReloadBehaviour>();

        _pluginLoader.ReloadAll();
    }

    internal static void Uninitialize()
    {
        if (_reloadBehavior != null)
        {
            UnityEngine.Object.Destroy(_reloadBehavior);
        }
    }

    private class ReloadBehaviour : UnityEngine.MonoBehaviour
    {
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(_keybinding))
            {
                BloodpebblePlugin.Logger.LogInfo("Reloading client plugins...");
                _pluginLoader.ReloadAll();
            }
        }
    }

}