
using Bloodpebble.Hooks;
using Bloodpebble.ReloadRequesting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEngine;

namespace Bloodpebble.Features;


internal class ReloadViaFileSystemChanges : BaseReloadRequestor
{
    private float _autoReloadDelaySeconds;
    private FileSystemWatcher _fileSystemWatcher;

    private bool _isPendingAutoReload = false;
    private float autoReloadTimer;

    internal ReloadViaFileSystemChanges(string reloadPluginsFolder, float autoReloadDelaySeconds)
    {
        _autoReloadDelaySeconds = autoReloadDelaySeconds;
        StartFileSystemWatcher(reloadPluginsFolder);
        GameFrame.OnLateUpdate += UpdateDebounce;
    }

    internal void Dispose()
    {
        GameFrame.OnLateUpdate -= UpdateDebounce;
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
            RequestFullReloadAsync();
        }
    }

}
