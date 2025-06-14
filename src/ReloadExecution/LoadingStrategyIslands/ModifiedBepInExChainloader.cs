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

namespace Bloodpebble.ReloadExecution.LoadingStrategyIslands
{
    // Extension of the BepInEx chainloader.
    //
    // Unfortunately, their chainloader is pretty locked-down visibility wise.
    // So we have some copy/paste/modified stuff in here, where inheritence isn't possible.
    //
    // We're also using our own extended PluginInfo in some places,
    // where we need to set fields, but bepinex has them set to internal visibility.
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

        /// <summary>
        /// A public bridge to access the protected plugin sorting utility.
        /// </summary>
        /// <param name="plugins">A list of plugins to sort by dependency.</param>
        /// <returns>A sorted list of plugins.</returns>
        public IEnumerable<BepInEx.PluginInfo> SortPluginList(IEnumerable<BepInEx.PluginInfo> plugins)
        {

            return ModifyLoadOrder(plugins.ToList());
        }

        public PluginInfo LoadPlugin(BepInEx.PluginInfo plugin, AssemblyLoadContext context, out Assembly assembly)
        {
            try
            {
                BloodpebblePlugin.Logger.Log(LogLevel.Info, $"Loading [{plugin}]");
                // Create and load a copy of the assembly, to prevent filesystem locks on the things we want to hot reload
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

                bloodpebblePlugin.Instance = base.LoadPlugin(plugin, assembly);
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

        // copied from the BaseChainloader, since that has private visibility
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
}