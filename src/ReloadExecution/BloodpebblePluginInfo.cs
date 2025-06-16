using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace Bloodpebble.ReloadExecution;


// This exists only to give us setter access to properties internal to BepInEx
public class BloodpebblePluginInfo : PluginInfo
{
    /// <summary>
    ///     General metadata about a plugin.
    /// </summary>
    public BepInPlugin __Metadata { get; internal set; }

    /// <summary>
    ///     Collection of <see cref="BepInProcess" /> attributes that describe what processes the plugin can run on.
    /// </summary>
    public IEnumerable<BepInProcess> __Processes { get; internal set; }

    /// <summary>
    ///     Collection of <see cref="BepInDependency" /> attributes that describe what plugins this plugin depends on.
    /// </summary>
    public IEnumerable<BepInDependency> __Dependencies { get; internal set; }

    /// <summary>
    ///     Collection of <see cref="BepInIncompatibility" /> attributes that describe what plugins this plugin
    ///     is incompatible with.
    /// </summary>
    public IEnumerable<BepInIncompatibility> __Incompatibilities { get; internal set; }

    /// <summary>
    ///     File path to the plugin DLL
    /// </summary>
    public string __Location { get; internal set; }

    /// <summary>
    ///     Instance of the plugin that represents this info. NULL if no plugin is instantiated from info (yet)
    /// </summary>
    public object __Instance { get; internal set; }

    public string __TypeName { get; internal set; }

    public BloodpebblePluginInfo(
        BepInPlugin metadata,
        IEnumerable<BepInProcess> processes,
        IEnumerable<BepInDependency> dependencies,
        IEnumerable<BepInIncompatibility> incompatibilities,
        string location,
        object instance,
        string typeName
    )
    {
        __Metadata = metadata;
        __Processes = processes;
        __Dependencies = dependencies;
        __Incompatibilities = incompatibilities;
        __Location = location;
        __Instance = instance;
        __TypeName = typeName;
    }

    public void SetInstance(object instance)
    {
        __Instance = instance;
    }

}

[HarmonyPatch]
public static class BloodpebblePluginInfoPatch
{
    [HarmonyPatch(typeof(PluginInfo), nameof(PluginInfo.Metadata), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Metadata_Get(PluginInfo __instance, ref BepInPlugin __result)
    {
        var bloodpebblePluginInfo = __instance as BloodpebblePluginInfo;
        if (bloodpebblePluginInfo is not null)
        {
            __result = bloodpebblePluginInfo.__Metadata;
        }
    }

    [HarmonyPatch(typeof(PluginInfo), nameof(PluginInfo.Processes), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Processes_Get(PluginInfo __instance, ref IEnumerable<BepInProcess> __result)
    {
        var bloodpebblePluginInfo = __instance as BloodpebblePluginInfo;
        if (bloodpebblePluginInfo is not null)
        {
            __result = bloodpebblePluginInfo.__Processes;
        }
    }

    [HarmonyPatch(typeof(PluginInfo), nameof(PluginInfo.Dependencies), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Dependencies_Get(PluginInfo __instance, ref IEnumerable<BepInDependency> __result)
    {
        var bloodpebblePluginInfo = __instance as BloodpebblePluginInfo;
        if (bloodpebblePluginInfo is not null)
        {
            __result = bloodpebblePluginInfo.__Dependencies;
        }
    }

    [HarmonyPatch(typeof(PluginInfo), nameof(PluginInfo.Incompatibilities), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Incompatibilities_Get(PluginInfo __instance, ref IEnumerable<BepInIncompatibility> __result)
    {
        var bloodpebblePluginInfo = __instance as BloodpebblePluginInfo;
        if (bloodpebblePluginInfo is not null)
        {
            __result = bloodpebblePluginInfo.__Incompatibilities;
        }
    }

    [HarmonyPatch(typeof(PluginInfo), nameof(PluginInfo.Location), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Location_Get(PluginInfo __instance, ref string __result)
    {
        var bloodpebblePluginInfo = __instance as BloodpebblePluginInfo;
        if (bloodpebblePluginInfo is not null)
        {
            __result = bloodpebblePluginInfo.__Location;
        }
    }

    [HarmonyPatch(typeof(PluginInfo), nameof(PluginInfo.Instance), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Instance_Get(PluginInfo __instance, ref object __result)
    {
        var bloodpebblePluginInfo = __instance as BloodpebblePluginInfo;
        if (bloodpebblePluginInfo is not null)
        {
            __result = bloodpebblePluginInfo.__Instance;
        }
    }

    [HarmonyPatch(typeof(PluginInfo), nameof(PluginInfo.TypeName), MethodType.Getter)]
    [HarmonyPostfix]
    public static void TypeName_Get(PluginInfo __instance, ref string __result)
    {
        var bloodpebblePluginInfo = __instance as BloodpebblePluginInfo;
        if (bloodpebblePluginInfo is not null)
        {
            __result = bloodpebblePluginInfo.__TypeName;
        }
    }
    
}
