using BepInEx;
using BepInEx.Unity.IL2CPP;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Bloodpebble.Reloading.LoaderIslands
{
    // Groups plugins into islands. Each island has its own AssemblyLoadContext.
    // Reloading a plugin in an island reloads every plugin in that island.
    class IslandsPluginLoader : IPluginLoader
    {
        private readonly Dictionary<string, AssemblyLoadContext> _pluginToContextMap = new();
        private readonly Dictionary<string, PluginInfo> _loadedPlugins = new();

        private ModifiedBepInExChainloader _bepinexChainloader = new();
        private PluginLoaderConfig _config;

        public IslandsPluginLoader(PluginLoaderConfig config)
        {
            _config = config;
        }

        public IList<PluginInfo> ReloadAll()
        {
            return LoadPlugins(_config.PluginsPath);
        }

        public IList<PluginInfo> ReloadGiven(IEnumerable<string> pluginGUIDs)
        {
            if (pluginGUIDs.Count() == 1)
            {
                List <PluginInfo> freshPlugins = new();
                if (TryReloadPlugin(pluginGUIDs.First(), out var freshPlugin))
                {
                    freshPlugins.Add(freshPlugin);
                }
                return freshPlugins;
            }
            else
            {
                // this could be improved
                return ReloadAll();
            }
        }

        public IList<PluginInfo> LoadPlugins(string pluginsPath)
        {
            UnloadAll();
            _bepinexChainloader = new ModifiedBepInExChainloader();

            var allPluginInfos = _bepinexChainloader.DiscoverAndSortPlugins(pluginsPath);
            var graph = BuildDependencyGraph(allPluginInfos);
            var pluginGroups = FindPluginGroups(allPluginInfos, graph);

            var newlyLoaded = new List<PluginInfo>();
            foreach (var group in pluginGroups)
            {
                try
                {
                    var loadedGroupPlugins = LoadGroup(group);
                    newlyLoaded.AddRange(loadedGroupPlugins);
                }
                catch (Exception ex)
                {
                    BloodpebblePlugin.Logger.LogError($"Failed to load a plugin group. Halting further loading. Error: {ex.Message}");
                    UnloadAll();
                    return new List<PluginInfo>();
                }
            }
            return newlyLoaded;
        }

        private List<PluginInfo> LoadGroup(List<BepInEx.PluginInfo> groupPlugins)
        {
            var newlyLoaded = new List<PluginInfo>();
            var sortedGroupPlugins = _bepinexChainloader.SortPluginList(groupPlugins);
            var groupContext = new AssemblyLoadContext($"BloodpebbleGroup-{Guid.NewGuid()}", isCollectible: true);
            var loadedAssembliesInContext = new Dictionary<string, Assembly>();

            groupContext.Resolving += (context, assemblyName) =>
            {
                if (loadedAssembliesInContext.TryGetValue(assemblyName.Name, out var foundAssembly))
                {
                    return foundAssembly;
                }
                return null;
            };

            foreach (var pluginInfo in sortedGroupPlugins)
            {
                var loadedPlugin = _bepinexChainloader.LoadPlugin(pluginInfo, groupContext, out var assembly);
                _loadedPlugins[loadedPlugin.Metadata.GUID] = loadedPlugin;
                _pluginToContextMap[loadedPlugin.Metadata.GUID] = groupContext;
                loadedAssembliesInContext[assembly.GetName().Name] = assembly;
                newlyLoaded.Add(loadedPlugin);
            }

            BloodpebblePlugin.Logger.LogInfo($"Successfully loaded plugin group with plugins: {string.Join(", ", newlyLoaded.Select(p => p.Metadata.Name))}");
            return newlyLoaded;
        }

        public void UnloadAll()
        {
            foreach (var pluginInfo in _loadedPlugins.Values)
            {
                try { (pluginInfo.Instance as BasePlugin)?.Unload(); }
                catch (Exception ex) { BloodpebblePlugin.Logger.LogError(ex); }
            }

            var allContexts = _pluginToContextMap.Values.Distinct().ToList();
            if (!allContexts.Any()) return;

            foreach (var context in allContexts)
            {
                context.Unload();
            }

            _pluginToContextMap.Clear();
            _loadedPlugins.Clear();
            _bepinexChainloader.Plugins.Clear();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            BloodpebblePlugin.Logger.LogInfo("All reloadable plugins have been unloaded.");
        }

        public bool TryReloadPlugin(string guid, [MaybeNullWhen(false)] out PluginInfo freshPlugin)
        {
            freshPlugin = null;
            if (!_pluginToContextMap.TryGetValue(guid, out var contextToUnload))
            {
                BloodpebblePlugin.Logger.LogError($"Cannot reload plugin with GUID '{guid}' because it is not loaded.");
                return false;
            }

            var groupToReload = _loadedPlugins.Values
                .Where(p => _pluginToContextMap.ContainsKey(p.Metadata.GUID) && _pluginToContextMap[p.Metadata.GUID] == contextToUnload)
                .ToList();

            var groupGuids = groupToReload.Select(p => p.Metadata.GUID).ToList();
            var groupFilePaths = groupToReload.Select(p => p.Location).ToList();

            BloodpebblePlugin.Logger.LogInfo($"Reload request for '{guid}'. Unloading its group: {string.Join(", ", groupGuids)}");

            foreach (var pluginInfo in groupToReload)
            {
                try
                {
                    (pluginInfo.Instance as BasePlugin)?.Unload();
                }
                catch (Exception ex)
                {
                    BloodpebblePlugin.Logger.LogError(ex);
                }
            }

            contextToUnload.Unload();

            foreach (var pluginGuid in groupGuids)
            {
                _pluginToContextMap.Remove(pluginGuid);
                _loadedPlugins.Remove(pluginGuid);

                _bepinexChainloader.Plugins.Remove(pluginGuid);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var tempReloadDir = Path.Combine(Path.GetTempPath(), $"BloodpebbleReload-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempReloadDir);

            try
            {
                foreach (var path in groupFilePaths)
                {
                    if (File.Exists(path))
                    {
                        File.Copy(path, Path.Combine(tempReloadDir, Path.GetFileName(path)));
                    }
                }

                var reloadDiscoverer = new ModifiedBepInExChainloader();
                var freshPluginInfos = reloadDiscoverer.DiscoverAndSortPlugins(tempReloadDir);

                BloodpebblePlugin.Logger.LogInfo($"Reloading group for '{guid}' from temporary location...");

                var reloadedGroup = LoadGroup(freshPluginInfos);
                freshPlugin = reloadedGroup.FirstOrDefault(p => p.Metadata.GUID == guid);
                return freshPlugin is not null;
            }
            finally
            {
                if (Directory.Exists(tempReloadDir))
                {
                    Directory.Delete(tempReloadDir, true);
                }
            }
        }

        private Dictionary<string, List<string>> BuildDependencyGraph(IEnumerable<BepInEx.PluginInfo> plugins)
        {
            var graph = new Dictionary<string, List<string>>();
            var pluginGuids = new HashSet<string>(plugins.Select(p => p.Metadata.GUID));

            foreach (var plugin in plugins)
            {
                if (!graph.ContainsKey(plugin.Metadata.GUID))
                    graph[plugin.Metadata.GUID] = new List<string>();

                foreach (var dep in plugin.Dependencies)
                {
                    if (pluginGuids.Contains(dep.DependencyGUID))
                    {
                        graph[plugin.Metadata.GUID].Add(dep.DependencyGUID);
                        if (!graph.ContainsKey(dep.DependencyGUID))
                            graph[dep.DependencyGUID] = new List<string>();
                        graph[dep.DependencyGUID].Add(plugin.Metadata.GUID);
                    }
                }
            }
            return graph;
        }

        private List<List<BepInEx.PluginInfo>> FindPluginGroups(IEnumerable<BepInEx.PluginInfo> plugins, Dictionary<string, List<string>> graph)
        {
            var groups = new List<List<BepInEx.PluginInfo>>();
            var visited = new HashSet<string>();
            var pluginDict = plugins.ToDictionary(p => p.Metadata.GUID);

            foreach (var plugin in plugins)
            {
                if (visited.Contains(plugin.Metadata.GUID)) continue;

                var currentGroupGuids = new HashSet<string>();
                var stack = new Stack<string>();
                stack.Push(plugin.Metadata.GUID);
                visited.Add(plugin.Metadata.GUID);

                while (stack.Count > 0)
                {
                    var currentGuid = stack.Pop();
                    currentGroupGuids.Add(currentGuid);

                    if (graph.TryGetValue(currentGuid, out var neighbors))
                    {
                        foreach (var neighbor in neighbors)
                        {
                            if (!visited.Contains(neighbor))
                            {
                                visited.Add(neighbor);
                                stack.Push(neighbor);
                            }
                        }
                    }
                }

                var currentGroup = currentGroupGuids.Select(guid => pluginDict[guid]).ToList();
                groups.Add(currentGroup);
            }

            return groups;
        }

    }
}