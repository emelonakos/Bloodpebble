using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Bloodpebble.ReloadExecution.LoadingStrategySilverBullet;

internal class BloodpebbleLoadContext : AssemblyLoadContext
{
    protected static Dictionary<string, Assembly> DefaultAssemblyLookupByFullName = new();
    private Dictionary<string, Assembly> _bloodpebbleAssemblyLookupByFullName;


    internal BloodpebbleLoadContext(
        string name,
        Dictionary<string, Assembly> assemblyLookupByFullName
    ) : base(name: name, isCollectible: true)
    {
        _bloodpebbleAssemblyLookupByFullName = assemblyLookupByFullName;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (_bloodpebbleAssemblyLookupByFullName.ContainsKey(assemblyName.FullName))
        {
            // We can't load an assembly from a different collectible context.
            // But we CAN return an assembly that's already loaded.
            return _bloodpebbleAssemblyLookupByFullName[assemblyName.FullName];
        }

        if (DefaultAssemblyLookupByFullName.TryGetValue(assemblyName.FullName, out var assembly))
        {
            return assembly;
        }

        try
        {
            assembly = Default.LoadFromAssemblyName(assemblyName);
        }
        catch (Exception ex)
        {
            CheckAndLogBadSearchForBloodpebblePlugin(assemblyName, ex);
            throw;
        }

        if (assembly != null)
        {
            DefaultAssemblyLookupByFullName.Add(assemblyName.FullName, assembly);
        }
        return assembly;
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
