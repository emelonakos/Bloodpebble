using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using BepInEx.Unity.IL2CPP;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using Unity.Collections;

namespace Bloodpebble.ReloadExecution.LoadingStrategySilverBullet;

internal class BloodpebbleLoadContext : AssemblyLoadContext
{
    protected static HashSet<string> LoopLocks = new();
    private Dictionary<string, BloodpebbleLoadContext> _loadContextLookup; // todo: don't need this in here. We get a Not Supported exception if trying to load from a collectible context
    private Dictionary<string, Assembly> _assemblyLookupByFullName;

    internal BloodpebbleLoadContext(
        string name,
        Dictionary<string, BloodpebbleLoadContext> loadContextLookup,
        Dictionary<string, Assembly> assemblyLookupByFullName
    ) : base(name: name, isCollectible: true)
    {
        _loadContextLookup = loadContextLookup;
        _assemblyLookupByFullName = assemblyLookupByFullName;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (_assemblyLookupByFullName.ContainsKey(assemblyName.FullName))
        {
            // We can't load an assembly from a different collectible context.
            // But we CAN return an assembly that's already loaded.
            return _assemblyLookupByFullName[assemblyName.FullName];
        }

        // todo: logs make it look like we might have to cache this ourselves
        var assembly = Default.LoadFromAssemblyName(assemblyName);
        if (assembly != null)
        {
            BloodpebblePlugin.Logger.LogDebug($"{Name} discovered {assemblyName.Name} in context {Default.Name}");
        }
        return assembly;
    }


}
