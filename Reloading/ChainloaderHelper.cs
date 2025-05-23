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

namespace Bloodpebble.Reloading;

// Contains a lot of copy/paste/modified stuff from the Bepinex BaseChainloader.
// Unfortunately, things are rather locked-down and not really intended for third-party extension,
// so we are doing some dirty stuff here to work around it.
class ChainloaderHelper : IL2CPPChainloader
{

    public new IList<BepInEx.PluginInfo> DiscoverPluginsFrom(string path, string cacheName = "chainloader")
    {
        return base.DiscoverPluginsFrom(path, cacheName);
    }


    public virtual new IList<BepInEx.PluginInfo> ModifyLoadOrder(IList<BepInEx.PluginInfo> plugins)
    {
        return base.ModifyLoadOrder(plugins);
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

                if (!loadedAssemblies.TryGetValue(plugin.Location, out var ass))
                    loadedAssemblies[plugin.Location] = ass = Assembly.LoadFrom(plugin.Location);

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
                TryRunModuleCtor(plugin, ass);
                bloodpebblePlugin.Instance = LoadPlugin(plugin, ass);
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

    protected static void TryRunModuleCtor(BepInEx.PluginInfo plugin, Assembly assembly)
    {
        try
        {
            RuntimeHelpers.RunModuleConstructor(assembly.GetType(plugin.TypeName).Module.ModuleHandle);
        }
        catch (Exception e)
        {
            BloodpebblePlugin.Logger.Log(LogLevel.Warning,
                       $"Couldn't run Module constructor for {assembly.FullName}::{plugin.TypeName}: {e}");
        }
    }
}