using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bloodpebble.Extensions;
using Il2CppSystem.Linq;

namespace Bloodpebble.ReloadExecution.LoadingStrategySilverBullet;


/// <summary>
///     A directed acyclic graph (DAG) to assist with plugin loading/unloading.
/// </summary>
/// <remarks>
///     <strong>Terminology</strong>
///     <br/>
///     Descendents: A vertex depends on these, directly or transitively.
///       (to load a plugin, it's descendents must already be loaded)
///     <br/>
///     Ancestors: A vertex is depended on by these, directly or transitively.
///        (to unload a plugin, it's ancestors must already be unloaded)
/// </remarks>
internal class DependencyGraph
{
    private HashSet<string> AddedVertexIds = new HashSet<string>();
    private Dictionary<string, HashSet<string>> DirectDependenciesLookup = new();
    private Dictionary<string, HashSet<string>> DescendentsLookup = new();
    private Dictionary<string, HashSet<string>> AncestorsLookup = new();

    internal bool AddVertex(string vertexId, HashSet<string> directDependencyIds)
    {
        if (AddedVertexIds.Contains(vertexId))
        {
            return false;
        }
        AddedVertexIds.Add(vertexId);
        DirectDependenciesLookup.Add(vertexId, directDependencyIds.ToHashSet());

        var descendentIds = DescendentsLookup.GetOrCreate(vertexId);
        descendentIds.UnionWith(directDependencyIds);
        var ancestorIds = AncestorsLookup.GetOrCreate(vertexId);

        // 1) collect list of own descendents, and update the descendents' lists of ancestors
        var visitedIds = new HashSet<string>();
        foreach (var visitingId in descendentIds)
        {
            // note: the ancestor list for vertexId should already be complete
            DfsWalkDescendentsForAdd(vertexId, descendentIds, ancestorIds, visitedIds, visitingId);
        }

        // 2) update own ancestors' lists of descendents
        foreach (var visitingId in ancestorIds)
        {
            var otherDescendentIds = DescendentsLookup.GetOrCreate(visitingId);
            otherDescendentIds.UnionWith(descendentIds);
            // note: no need to dfs; the ancestor list for vertexId should already be complete
        }

        return true;
    }

    private void DfsWalkDescendentsForAdd(string ownVertexId, HashSet<string> ownDescendentIds, HashSet<string> ownAncestorIds, HashSet<string> visitedIds, string otherId)
    {
        if (visitedIds.Contains(otherId))
        {
            return;
        }
        visitedIds.Add(otherId);

        var otherAncestorIds = AncestorsLookup.GetOrCreate(otherId);
        otherAncestorIds.Add(ownVertexId);
        otherAncestorIds.UnionWith(ownAncestorIds);

        var otherDescendentIds = DescendentsLookup.GetOrCreate(otherId);
        ownDescendentIds.UnionWith(otherDescendentIds);
        foreach (var deeperVertexId in otherDescendentIds)
        {
            DfsWalkDescendentsForAdd(ownVertexId, ownDescendentIds, ownAncestorIds, visitedIds, deeperVertexId);
        }
    }

    internal bool RemoveVertex(string vertexId)
    {
        if (!AddedVertexIds.Contains(vertexId))
        {
            return false;
        }

        var descendentIds = DescendentsLookup[vertexId];
        foreach (var descendentId in descendentIds)
        {
            if (AncestorsLookup.TryGetValue(descendentId, out var otherAncestorIds))
            {
                otherAncestorIds.Remove(vertexId);
            }
        }

        var ancestorIds = AncestorsLookup[vertexId];
        foreach (var ancestorId in ancestorIds)
        {
            if (DescendentsLookup.TryGetValue(ancestorId, out var otherDescenentIds))
            {
                otherDescenentIds.Remove(vertexId);
            }
        }

        AddedVertexIds.Remove(vertexId);
        DescendentsLookup.Remove(vertexId);
        AncestorsLookup.Remove(vertexId);
        DirectDependenciesLookup.Remove(vertexId);
        return true;
    }

    internal ISet<string> FindAllVertexesToLoad(ISet<string> targetedVertexIdsToLoad)
    {
        var vertexIdsToLoad = new HashSet<string>();
        vertexIdsToLoad.UnionWith(targetedVertexIdsToLoad);
        foreach (var vertexId in targetedVertexIdsToLoad)
        {
            if (DescendentsLookup.TryGetValue(vertexId, out var descendentIds))
            {
                vertexIdsToLoad.UnionWith(descendentIds);
            }
        }
        return vertexIdsToLoad;
    }

    internal ISet<string> FindAllVertexesToUnload(ISet<string> targetedVertexIdsToUnload)
    {
        var vertexIdsToUnload = new HashSet<string>();
        vertexIdsToUnload.UnionWith(targetedVertexIdsToUnload);
        foreach (var vertexId in targetedVertexIdsToUnload)
        {
            if (AncestorsLookup.TryGetValue(vertexId, out var ancestorIds))
            {
                vertexIdsToUnload.UnionWith(ancestorIds);
            }
        }
        return vertexIdsToUnload;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine("===========================================");
        sb.AppendLine();

        sb.AppendLine("VertexIds ---------------------------------");
        if (!AddedVertexIds.Any())
        {
            sb.AppendLine("  (empty)");
        }
        foreach (var id in AddedVertexIds)
        {
            sb.AppendLine($"  {id}");
        }
        sb.AppendLine();

        sb.AppendLine("DirectDependenciesLookup ------------------");
        if (!DirectDependenciesLookup.Any())
        {
            sb.AppendLine("  (empty)");
        }
        foreach (var (id, dependencyIds) in DirectDependenciesLookup)
        {
            sb.AppendLine($"  {id}");
            if (!dependencyIds.Any())
            {
                sb.AppendLine("    (empty)");
            }
            foreach (var dependencyId in dependencyIds)
            {
                sb.AppendLine($"    {dependencyId}");
            }
        }
        sb.AppendLine();

        sb.AppendLine("DescendentsLookup --------------------------");
        if (!DescendentsLookup.Any())
        {
            sb.AppendLine("  (empty)");
        }
        foreach (var (id, descendentIds) in DescendentsLookup)
        {
            sb.AppendLine($"  {id}");
            if (!descendentIds.Any())
            {
                sb.AppendLine("    (empty)");
            }
            foreach (var descendentId in descendentIds)
            {
                sb.AppendLine($"    {descendentId}");
            }
        }
        sb.AppendLine();

        sb.AppendLine("AncestorsLookup ----------------------------");
        if (!AncestorsLookup.Any())
        {
            sb.AppendLine("  (empty)");
        }
        foreach (var (id, ancestorIds) in AncestorsLookup)
        {
            sb.AppendLine($"  {id}");
            if (!ancestorIds.Any())
            {
                sb.AppendLine("    (empty)");
            }
            foreach (var ancestorId in ancestorIds)
            {
                sb.AppendLine($"    {ancestorId}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("===========================================");
        return sb.ToString();
    }

}
