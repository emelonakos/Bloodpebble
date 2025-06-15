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
    private Dictionary<string, Assembly> _assemblyLookupByFullName = new();
    private Dictionary<string, Assembly> _assemblyLookupByPluginGuid = new();

    public ModifiedBepInExChainloader()
    {
        _assemblyResolver = new DefaultAssemblyResolver();
        _assemblyResolver.AddSearchDirectory(Paths.ManagedPath);
        _assemblyResolver.AddSearchDirectory(Paths.BepInExAssemblyDirectory);
        _assemblyResolver.AddSearchDirectory(Path.Combine(Paths.BepInExRootPath, "interop"));
    }

    public void UnloadPluginAssembly(string pluginGuid)
    {
        if (_loadContextLookupByPluginGuid.Remove(pluginGuid, out var loadContext))
        {
            loadContext.Unload();
        }

        if (_assemblyLookupByPluginGuid.Remove(pluginGuid, out var assembly))
        {
            if (assembly.FullName is null)
            {
                BloodpebblePlugin.Logger.LogWarning($"Assembly for {pluginGuid} has no FullName.");
            }
            else
            {
                _assemblyLookupByFullName.Remove(assembly.FullName);
            }
        }
    }

    private BloodpebbleLoadContext CreateNewAssemblyLoadContext(string pluginGuid)
    {
        return new BloodpebbleLoadContext(name: $"BloodpebbleContext-{pluginGuid}", _assemblyLookupByFullName);
    }

    public IList<PluginInfo> LoadPlugins(IList<BepInEx.PluginInfo> plugins)
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
                    _loadContextLookupByPluginGuid[pluginGuid] = loadContext;

                    // Create and load a copy of the assembly, to prevent filesystem locks on the things we want to hot reload
                    using var dll = AssemblyDefinition.ReadAssembly(plugin.Location, new() { AssemblyResolver = _assemblyResolver });
                    using var ms = new MemoryStream();
                    dll.Write(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    loadedAssemblies[plugin.Location] = assembly = loadContext.LoadFromStream(ms);

                    _assemblyLookupByPluginGuid.Add(pluginGuid, assembly);
                    _assemblyLookupByFullName.Add(assembly.FullName, assembly);
                }

                var bloodpebblePlugin = new PluginInfo
                {
                    Metadata = plugin.Metadata,
                    Processes = plugin.Processes,
                    Dependencies = plugin.Dependencies,
                    Incompatibilities = plugin.Incompatibilities,
                    Location = plugin.Location,
                    Instance = plugin.Instance,
                    TypeName = plugin.TypeName,
                };
                Plugins[plugin.Metadata.GUID] = bloodpebblePlugin;
                TryRunModuleCtor(plugin, assembly);

                var doesMetadataIndicateUnloadable = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .Where(att => att.Key.ToLowerInvariant().Equals("reloadable"))
                    .Where(att => att.Value?.ToLowerInvariant().Equals("true") ?? false)
                    .Any();

                BloodpebblePlugin.Logger.Log(LogLevel.Info, $"{assembly.GetName().Name} metadata indicates reloadable: {doesMetadataIndicateUnloadable}");

                bloodpebblePlugin.Instance = LoadPlugin(plugin, assembly);
                loadedPlugins.Add(bloodpebblePlugin);

                // PluginLoaded?.Invoke(bloodpebblePlugin);
            }
            catch (Exception ex)
            {
                invalidPlugins.Add(plugin.Metadata.GUID);
                Plugins.Remove(plugin.Metadata.GUID);

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
        var discoveredPlugins = new List<BepInEx.PluginInfo>();
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

    protected static void TryRunModuleCtor(BepInEx.PluginInfo plugin, Assembly assembly)
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

}