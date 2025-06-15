using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

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
        assembly = Default.LoadFromAssemblyName(assemblyName);
        if (assembly != null)
        {
            DefaultAssemblyLookupByFullName.Add(assemblyName.FullName, assembly);
        }
        return assembly;
    }


}
