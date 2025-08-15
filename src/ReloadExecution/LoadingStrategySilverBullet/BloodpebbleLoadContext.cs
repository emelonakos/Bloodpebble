using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Bloodpebble.ReloadExecution.LoadingStrategySilverBullet;

internal class BloodpebbleLoadContext : AssemblyLoadContext
{
    protected static Dictionary<string, Assembly> DefaultAssemblyLookupByFullName = new();
    private Dictionary<string, Assembly> _bloodpebbleAssemblyLookupByPartialName;


    internal BloodpebbleLoadContext(
        string name,
        Dictionary<string, Assembly> assemblyLookupByPartialName
    ) : base(name: name, isCollectible: true)
    {
        _bloodpebbleAssemblyLookupByPartialName = assemblyLookupByPartialName;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // We can't load an assembly from a different collectible context.
        // But we CAN return an assembly that's already loaded.
        var assembly = LoadAssembly_PreloadedBloodpebblePlugin(assemblyName);
        assembly ??= LoadAssembly_NotPloodpebblePlugin(assemblyName);
        return assembly;
    }

    private Assembly? LoadAssembly_PreloadedBloodpebblePlugin(AssemblyName assemblyName)
    {
        if (assemblyName.Name is null)
        {
            return null;
        }
        if (_bloodpebbleAssemblyLookupByPartialName.TryGetValue(assemblyName.Name, out var assembly))
        {
            return assembly;
        }
        return null;
    }

    private Assembly? LoadAssembly_NotPloodpebblePlugin(AssemblyName assemblyName)
    {
        if (DefaultAssemblyLookupByFullName.TryGetValue(assemblyName.FullName, out var assembly))
        {
            return assembly;
        }

        try
        {
            assembly = Default.LoadFromAssemblyName(assemblyName);
            if (assembly != null)
            {
                DefaultAssemblyLookupByFullName.Add(assemblyName.FullName, assembly);
            }
            return assembly;
        }
        catch (Exception ex)
        {
            CheckAndLogBadSearchForBloodpebblePlugin(assemblyName, ex);
            throw;
        }
    }

    private void CheckAndLogBadSearchForBloodpebblePlugin(AssemblyName assemblyName, Exception ex)
    {
        Exception? inspectedException = ex;
        while (inspectedException is not null)
        {
            if (inspectedException.Message.Equals("Resolving to a collectible assembly is not supported."))
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Potential fallback to default assembly resolution, for a plugin that was already loaded by Bloodpebble.");
                sb.AppendLine($"  Assembly load context triggering the search: {Name}");
                sb.Append($"  Assembly being searched for: {assemblyName.FullName}");
                BloodpebblePlugin.Logger.LogWarning(sb.ToString());
                break;
            }
            inspectedException = ex.InnerException;
        }
    }

}
