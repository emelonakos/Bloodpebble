using BepInEx;
using BepInEx.Unity.IL2CPP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil;
using BepInEx.Bootstrap;
using Unity.Collections;
using System.Runtime.Loader;
using System.Diagnostics.CodeAnalysis;

namespace Bloodpebble.ReloadExecution.LoadingStrategySilverBullet;

// Extension of the BepInEx chainloader.
//
// Unfortunately, their chainloader is pretty locked-down visibility wise.
// So we have some copy/paste/modified stuff in here, where inheritence isn't possible.
//
// We're also using our own extended PluginInfo in some places,
// where we need to set fields, but bepinex has them set to internal visibility.
class ModifiedBepInExChainloader : IL2CPPChainloader
{
    private BaseAssemblyResolver _assemblyResolver;

    private Dictionary<string, BloodpebbleLoadContext> _loadContextLookupByPluginGuid = new();
    private Dictionary<string, Assembly> _assemblyLookupByPartialName = new();
    private Dictionary<string, Assembly> _assemblyLookupByPluginGuid = new();

    public ModifiedBepInExChainloader()
    {
        _assemblyResolver = new DefaultAssemblyResolver();
        _assemblyResolver.AddSearchDirectory(Paths.ManagedPath);
        _assemblyResolver.AddSearchDirectory(Paths.BepInExAssemblyDirectory);
        _assemblyResolver.AddSearchDirectory(Path.Combine(Paths.BepInExRootPath, "interop"));
    }

    private BloodpebbleLoadContext CreateNewAssemblyLoadContext(string pluginGuid)
    {
        return new BloodpebbleLoadContext(name: $"BloodpebbleContext-{pluginGuid}", _assemblyLookupByPartialName);
    }

    /// <summary>
    /// Discovers all plugins in the plugin directory without loading them.
    /// </summary>
    /// <remarks>
    /// This is useful for discovering BepInEx plugin metadata.
    /// </remarks>
    /// <param name="path">Path from which to search the plugins.</param>
    /// <param name="cacheName">Cache name to use. If null, results are not cached.</param>
    /// <returns>List of discovered plugins and their metadata.</returns>
    public IList<PluginInfo> DiscoverPluginsFrom(string path)
    {
        return base.DiscoverPluginsFrom(path);
    }

    /// <summary>
    /// Preprocess the plugins and modify the load order. (public bridge to protected method)
    /// </summary>
    /// <remarks>Some plugins may be skipped if they cannot be loaded (wrong metadata, etc).</remarks>
    /// <param name="plugins">Plugins to process.</param>
    /// <returns>List of plugins to load in the correct load order.</returns>
    public IEnumerable<PluginInfo> ModifyLoadOrder(IEnumerable<PluginInfo> plugins)
    {
        return base.ModifyLoadOrder(plugins.ToList());
    }

    /// <summary>
    /// Modifies the order of given plugins for unloading.
    /// </summary>
    /// <param name="plugins">Plugins to process.</param>
    /// <returns>List of plugins to load in the correct unload order.</returns>
    public IEnumerable<PluginInfo> ModifyUnloadOrder(IEnumerable<PluginInfo> plugins)
    {
        // We use a sorted dictionary to ensure consistent order
        var dependencyDict = new SortedDictionary<string, IEnumerable<string>>(StringComparer.InvariantCultureIgnoreCase);
        var pluginsByGuid = new Dictionary<string, PluginInfo>();

        foreach (var plugin in plugins)
        {
            // nb: all the extra proccessing from ModifyLoadOrder isn't necessary, because the plugin is assumed to already be loaded
            pluginsByGuid[plugin.Metadata.GUID] = plugin;
            dependencyDict[plugin.Metadata.GUID] = plugin.Dependencies.Select(d => d.DependencyGUID);
        }

        // Sort plugins by their dependencies.
        // Give missing dependencies no dependencies of their own,
        // which will cause missing plugins to be first in the resulting list (and then last after reversing).
        var emptyDependencies = new string[0];
        var GetDependencies = (string guid) => dependencyDict.GetValueOrDefault(guid) ?? emptyDependencies;
        var sortedForLoading = Utility.TopologicalSort(dependencyDict.Keys, GetDependencies);
        var sortedForUnloading = sortedForLoading.Reverse();
        return sortedForUnloading.Where(pluginsByGuid.ContainsKey).Select(guid => pluginsByGuid[guid]).ToList();
    }

    public IList<PluginInfo> LoadPlugins(IList<PluginInfo> plugins)
    {
        var sortedPlugins = ModifyLoadOrder(plugins);

        var invalidPlugins = new HashSet<string>();
        var processedPlugins = new Dictionary<string, SemanticVersioning.Version>();
        var loadedAssemblies = new Dictionary<string, Assembly>();
        var loadedPlugins = new List<PluginInfo>();

        foreach (var plugin in sortedPlugins)
        {
            var dependsOnInvalidPlugin = false;
            var missingDependencies = new List<BepInDependency>();
            foreach (var dependency in plugin.Dependencies)
            {
                static bool IsHardDependency(BepInDependency dep) =>
                    (dep.Flags & BepInDependency.DependencyFlags.HardDependency) != 0;

                // If the dependency wasn't already processed, it's missing altogether
                var dependencyExists =
                    processedPlugins.TryGetValue(dependency.DependencyGUID, out var pluginVersion);
                // Alternatively, if the dependency hasn't been loaded before, it's missing too
                if (!dependencyExists)
                {
                    dependencyExists = Plugins.TryGetValue(dependency.DependencyGUID, out var pluginInfo);
                    pluginVersion = pluginInfo?.Metadata.Version;
                }

                if (!dependencyExists || dependency.VersionRange != null &&
                    !dependency.VersionRange.IsSatisfied(pluginVersion))
                {
                    // If the dependency is hard, collect it into a list to show
                    if (IsHardDependency(dependency))
                        missingDependencies.Add(dependency);
                    continue;
                }

                // If the dependency is a hard and is invalid (e.g. has missing dependencies), report that to the user
                if (invalidPlugins.Contains(dependency.DependencyGUID) && IsHardDependency(dependency))
                {
                    dependsOnInvalidPlugin = true;
                    break;
                }
            }

            processedPlugins.Add(plugin.Metadata.GUID, plugin.Metadata.Version);

            if (dependsOnInvalidPlugin)
            {
                var message =
                    $"Skipping [{plugin}] because it has a dependency that was not loaded. See previous errors for details.";
                DependencyErrors.Add(message);
                BloodpebblePlugin.Logger.Log(LogLevel.Warning, message);
                continue;
            }

            if (missingDependencies.Count != 0)
            {
                var message = $@"Could not load [{plugin}] because it has missing dependencies: {string.Join(", ", missingDependencies.Select(s => s.VersionRange == null ? s.DependencyGUID : $"{s.DependencyGUID} ({s.VersionRange})").ToArray())}";
                DependencyErrors.Add(message);
                BloodpebblePlugin.Logger.Log(LogLevel.Error, message);

                invalidPlugins.Add(plugin.Metadata.GUID);
                continue;
            }

            try
            {
                BloodpebblePlugin.Logger.Log(LogLevel.Info, $"Loading [{plugin}]");

                if (!loadedAssemblies.TryGetValue(plugin.Location, out var assembly))
                {
                    var pluginGuid = plugin.Metadata.GUID;
                    var loadContext = CreateNewAssemblyLoadContext(pluginGuid);

                    // Create and load a copy of the assembly, to prevent filesystem locks on the things we want to hot reload
                    using var dll = AssemblyDefinition.ReadAssembly(plugin.Location, new() { AssemblyResolver = _assemblyResolver });
                    using var ms = new MemoryStream();
                    dll.Write(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = loadContext.LoadFromStream(ms); 

                    if (assembly.FullName is null)
                    {
                        BloodpebblePlugin.Logger.Log(LogLevel.Error, $"Assembly.FullName is null for plugin [{plugin}]");
                        continue;
                    }

                    var partialName = assembly.GetName().Name;
                    if (partialName is null)
                    {
                        BloodpebblePlugin.Logger.Log(LogLevel.Error, $"Assembly.GetName().Name is null for plugin [{plugin}]");
                        continue;
                    }

                    loadedAssemblies[plugin.Location] = assembly;

                    _loadContextLookupByPluginGuid[pluginGuid] = loadContext;
                    _assemblyLookupByPluginGuid.Add(pluginGuid, assembly);
                    _assemblyLookupByPartialName.Add(partialName, assembly);
                }

                var bloodpebblePlugin = new BloodpebblePluginInfo(
                    metadata: plugin.Metadata,
                    processes: plugin.Processes,
                    dependencies: plugin.Dependencies,
                    incompatibilities: plugin.Incompatibilities,
                    location: plugin.Location,
                    instance: plugin.Instance,
                    typeName: plugin.TypeName
                );
                Plugins[plugin.Metadata.GUID] = bloodpebblePlugin;
                TryRunModuleCtor(plugin, assembly);

                var doesMetadataIndicateUnloadable = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .Where(att => att.Key.ToLowerInvariant().Equals("reloadable"))
                    .Where(att => att.Value?.ToLowerInvariant().Equals("true") ?? false)
                    .Any();

                BloodpebblePlugin.Logger.Log(LogLevel.Info, $"{assembly.GetName().Name} metadata indicates reloadable: {doesMetadataIndicateUnloadable}");

                var pluginInstance = LoadPlugin(plugin, assembly);
                bloodpebblePlugin.SetInstance(pluginInstance);
                loadedPlugins.Add(bloodpebblePlugin);

                // PluginLoaded?.Invoke(bloodpebblePlugin);
            }
            catch (Exception ex)
            {
                invalidPlugins.Add(plugin.Metadata.GUID);
                Plugins.Remove(plugin.Metadata.GUID);
                UnloadPluginAssembly(plugin.Metadata.GUID);

                BloodpebblePlugin.Logger.Log(LogLevel.Error,
                           $"Error loading [{plugin}]: {(ex is ReflectionTypeLoadException re ? TypeLoader.TypeLoadExceptionToString(re) : ex.ToString())}");
            }
        }

        return loadedPlugins;
    }

    /// <summary>
    /// Detects and loads all plugins in the specified directories.
    /// </summary>
    /// <remarks>
    /// It is better to collect all paths at once and use a single call to LoadPlugins than multiple calls.
    /// This allows to run proper dependency resolving and to load all plugins in one go.
    /// </remarks>
    /// <param name="pluginsPaths">Directories to search the plugins from.</param>
    /// <returns>List of loaded plugin infos.</returns>
    public new IList<PluginInfo> LoadPlugins(params string[] pluginsPaths)
    {
        var discoveredPlugins = new List<PluginInfo>();
        foreach (var pluginsPath in pluginsPaths)
        {
            discoveredPlugins.AddRange(DiscoverPluginsFrom(pluginsPath));
            _assemblyResolver.AddSearchDirectory(pluginsPath);
        }
        
        var loadedPlugins = LoadPlugins(discoveredPlugins);

        foreach (var pluginsPath in pluginsPaths)
        {
            _assemblyResolver.RemoveSearchDirectory(pluginsPath);
        }

        return loadedPlugins;

    }

    protected static void TryRunModuleCtor(PluginInfo plugin, Assembly assembly)
    {
        try
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            RuntimeHelpers.RunModuleConstructor(assembly.GetType(plugin.TypeName).Module.ModuleHandle);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
        catch (Exception e)
        {
            BloodpebblePlugin.Logger.Log(LogLevel.Warning,
                       $"Couldn't run Module constructor for {assembly.FullName}::{plugin.TypeName}: {e}");
        }
    }

    public void UnloadPlugin(PluginInfo plugin)
    {
        var pluginInstance = (BasePlugin)plugin.Instance;
        var assemblyName = pluginInstance.GetType().Assembly.GetName();
        var pluginName = $"{assemblyName.Name} {assemblyName.Version}";
        try
        {
            bool unloadSuccessful = pluginInstance.Unload();
            if (!unloadSuccessful)
            {
                BloodpebblePlugin.Logger.LogWarning($"Plugin {pluginName} might not be reloadable. (Plugin.Unload returned false)");
            }
        }
        catch (Exception ex)
        {
            BloodpebblePlugin.Logger.LogError($"Error unloading plugin {pluginName}: {ex}");
        }
        UnloadPluginAssembly(plugin.Metadata.GUID);
        Plugins.Remove(plugin.Metadata.GUID);
    }

    public void UnloadPluginAssembly(string pluginGuid)
    {
        if (_loadContextLookupByPluginGuid.Remove(pluginGuid, out var loadContext))
        {
            loadContext.Unload();
        }

        if (_assemblyLookupByPluginGuid.Remove(pluginGuid, out var assembly))
        {
            var partialName = assembly.GetName().Name;
            if (partialName is null)
            {
                BloodpebblePlugin.Logger.LogWarning($"Assembly for {pluginGuid} has no Name.");
            }
            else
            {
                _assemblyLookupByPartialName.Remove(partialName);
            }
        }
    }

}