using BepInEx;
using BepInEx.Unity.IL2CPP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using Mono.Cecil;
using BepInEx.Bootstrap;
using System.Runtime.Loader;

namespace Bloodpebble.Reloading;

class ModifiedBepInExChainloader : IL2CPPChainloader
{
    private readonly BaseAssemblyResolver _assemblyResolver;

    public ModifiedBepInExChainloader()
    {
        _assemblyResolver = new DefaultAssemblyResolver();
        _assemblyResolver.AddSearchDirectory(Paths.ManagedPath);
        _assemblyResolver.AddSearchDirectory(Paths.BepInExAssemblyDirectory);
        _assemblyResolver.AddSearchDirectory(Path.Combine(Paths.BepInExRootPath, "interop"));
    }

    public List<BepInEx.PluginInfo> DiscoverAndSortPlugins(string pluginsPath)
    {
        var discovered = DiscoverPluginsFrom(pluginsPath).ToList();
        if (discovered.Any())
        {
            _assemblyResolver.AddSearchDirectory(pluginsPath);
        }
        var sortedList = ModifyLoadOrder(discovered);
        return sortedList.ToList(); 
    }

    public PluginInfo LoadPlugin(BepInEx.PluginInfo plugin, AssemblyLoadContext context, out Assembly assembly)
    {
        try
        {
            BloodpebblePlugin.Logger.Log(LogLevel.Info, $"Loading [{plugin}]");

            using var dll = AssemblyDefinition.ReadAssembly(plugin.Location, new() { AssemblyResolver = _assemblyResolver });
            using var ms = new MemoryStream();
            dll.Write(ms);
            ms.Seek(0, SeekOrigin.Begin);
            assembly = context.LoadFromStream(ms);

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

            bloodpebblePlugin.Instance = LoadPlugin(plugin, assembly);
            return bloodpebblePlugin;
        }
        catch (Exception ex)
        {
            Plugins.Remove(plugin.Metadata.GUID);
            BloodpebblePlugin.Logger.Log(LogLevel.Error,
                $"Error loading [{plugin}]: {(ex is ReflectionTypeLoadException re ? TypeLoader.TypeLoadExceptionToString(re) : ex.ToString())}");
            throw;
        }
    }

    public void RemoveSearchDirectory(string pluginsPath)
    {
        _assemblyResolver.RemoveSearchDirectory(pluginsPath);
    }

    protected static void TryRunModuleCtor(BepInEx.PluginInfo plugin, Assembly assembly)
    {
        try
        {
#pragma warning disable CS8602
            RuntimeHelpers.RunModuleConstructor(assembly.GetType(plugin.TypeName).Module.ModuleHandle);
#pragma warning restore CS8602
        }
        catch (Exception e)
        {
            BloodpebblePlugin.Logger.Log(LogLevel.Warning,
                $"Couldn't run Module constructor for {assembly.FullName}::{plugin.TypeName}: {e}");
        }
    }
}