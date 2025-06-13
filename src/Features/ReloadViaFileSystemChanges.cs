
using Bloodpebble.ReloadRequesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEngine;

namespace Bloodpebble.Features;

// todo: defer reloading to happen outside of the system updates.
internal class ReloadViaFileSystemChanges : BaseReloadRequestor
{
    private float _autoReloadDelaySeconds;
    private TickBehaviour _tickBehaviour;
    private FileSystemWatcher _fileSystemWatcher;

    private bool _isPendingAutoReload = false;
    private float autoReloadTimer;

    internal ReloadViaFileSystemChanges(string reloadPluginsFolder, float autoReloadDelaySeconds)
    {
        _autoReloadDelaySeconds = autoReloadDelaySeconds;

        _tickBehaviour = BloodpebblePlugin.Instance.AddComponent<TickBehaviour>();
        _tickBehaviour.Updating += UpdateDebounce;

        StartFileSystemWatcher(reloadPluginsFolder);
    }

    internal void Dispose()
    {
        if (_tickBehaviour != null)
        {
            _tickBehaviour.Updating -= UpdateDebounce;
            UnityEngine.Object.Destroy(_tickBehaviour);
        }
    }

    [MemberNotNull(nameof(_fileSystemWatcher))]
    private void StartFileSystemWatcher(string reloadPluginsFolder)
    {
        _fileSystemWatcher = new FileSystemWatcher(reloadPluginsFolder);
        _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
        _fileSystemWatcher.Filter = "*.dll";
        _fileSystemWatcher.Changed += FileChangedEventHandler;
        _fileSystemWatcher.Deleted += FileChangedEventHandler;
        _fileSystemWatcher.Created += FileChangedEventHandler;
        _fileSystemWatcher.Renamed += FileChangedEventHandler;
        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    private void FileChangedEventHandler(object sender, FileSystemEventArgs args)
    {
        _isPendingAutoReload = true;
        autoReloadTimer = _autoReloadDelaySeconds;
    }

    private void UpdateDebounce()
    {
        if (!_isPendingAutoReload)
        {
            return;
        }

        autoReloadTimer -= Time.unscaledDeltaTime;
        if (autoReloadTimer <= .0f)
        {
            _isPendingAutoReload = false;
            BloodpebblePlugin.Logger.LogInfo("Automatically reloading plugins..."); // todo: this logging should be moved to the handler, not the requester
            RequestFullReloadAsync();
        }
    }
    
    private class TickBehaviour : UnityEngine.MonoBehaviour
    {
        public event Action? Updating;

        public void Update()
        {
            Updating?.Invoke();
        }
    }

}
