using System;
using System.Collections.Generic;
using BepInEx;

namespace Bloodpebble.Reloading;

#nullable disable

// workaround to let us do things normally restricted to BepInEx internals. e.g. setting properties
class PluginInfo : BepInEx.PluginInfo
{
    /// <summary>
    ///     General metadata about a plugin.
    /// </summary>
    public new BepInPlugin Metadata { get; internal set; }

    /// <summary>
    ///     Collection of <see cref="BepInProcess" /> attributes that describe what processes the plugin can run on.
    /// </summary>
    public new IEnumerable<BepInProcess> Processes { get; internal set; }

    /// <summary>
    ///     Collection of <see cref="BepInDependency" /> attributes that describe what plugins this plugin depends on.
    /// </summary>
    public new IEnumerable<BepInDependency> Dependencies { get; internal set; }

    /// <summary>
    ///     Collection of <see cref="BepInIncompatibility" /> attributes that describe what plugins this plugin
    ///     is incompatible with.
    /// </summary>
    public new IEnumerable<BepInIncompatibility> Incompatibilities { get; internal set; }

    /// <summary>
    ///     File path to the plugin DLL
    /// </summary>
    public new string Location { get; internal set; }

    /// <summary>
    ///     Instance of the plugin that represents this info. NULL if no plugin is instantiated from info (yet)
    /// </summary>
    public new object Instance { get; internal set; }

    public new string TypeName { get; internal set; }

    internal Version TargettedBepInExVersion { get; set; }
}
