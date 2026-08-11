namespace OrganizationService.Domain;

public static class HierarchyValidator
{
    public static void EnsureNoCircularParent(IReadOnlyDictionary<Guid, Guid?> nodes, Guid nodeId, Guid? newParentId)
    {
        if (!newParentId.HasValue) return;
        if (newParentId.Value == nodeId)
            throw new InvalidOperationException("A node cannot be its own parent.");
        var visited = new HashSet<Guid> { nodeId };
        var current = newParentId;
        while (current.HasValue)
        {
            if (!visited.Add(current.Value))
                throw new InvalidOperationException("Circular hierarchy detected.");
            nodes.TryGetValue(current.Value, out var next);
            current = next;
        }
    }
}
