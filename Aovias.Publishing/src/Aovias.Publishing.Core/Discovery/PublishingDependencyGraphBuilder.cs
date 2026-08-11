namespace Aovias.Publishing.Core.Discovery;

/// <summary>
/// Builds deterministic startup ordering and validates dependency graphs from contract metadata.
/// </summary>
public static class PublishingDependencyGraphBuilder
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildGraph(
        IEnumerable<(string ServiceId, IReadOnlyList<string> Dependencies)> services)
    {
        var graph = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (serviceId, dependencies) in services)
        {
            graph[serviceId] = dependencies;
            foreach (var dependency in dependencies)
            {
                graph.TryAdd(dependency, []);
            }
        }

        return graph;
    }

    public static IReadOnlyList<string> TopologicalSort(IReadOnlyDictionary<string, IReadOnlyList<string>> graph)
    {
        DetectCircularDependencies(graph);

        var indegree = graph.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count,
            StringComparer.OrdinalIgnoreCase);

        foreach (var dependencies in graph.Values)
        {
            foreach (var dependency in dependencies)
            {
                indegree.TryAdd(dependency, 0);
            }
        }

        var ready = indegree
            .Where(pair => pair.Value == 0)
            .Select(pair => pair.Key)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ordered = new List<string>();
        while (ready.Count > 0)
        {
            var current = ready[0];
            ready.RemoveAt(0);
            ordered.Add(current);

            foreach (var (serviceId, dependencies) in graph)
            {
                if (!dependencies.Contains(current, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                indegree[serviceId]--;
                if (indegree[serviceId] == 0)
                {
                    InsertSorted(ready, serviceId);
                }
            }
        }

        if (ordered.Count != indegree.Count)
        {
            throw new InvalidOperationException("Circular dependency detected in deployment graph.");
        }

        return ordered;
    }

    public static int GetStartupOrder(string serviceId, IReadOnlyList<string> topologicalOrder)
    {
        for (var index = 0; index < topologicalOrder.Count; index++)
        {
            if (topologicalOrder[index].Equals(serviceId, StringComparison.OrdinalIgnoreCase))
            {
                return (index + 1) * 10;
            }
        }

        return int.MaxValue;
    }

    public static void DetectCircularDependencies(IReadOnlyDictionary<string, IReadOnlyList<string>> graph)
    {
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var serviceId in graph.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
        {
            Visit(serviceId);
        }

        return;

        void Visit(string serviceId)
        {
            if (visited.Contains(serviceId))
            {
                return;
            }

            if (!visiting.Add(serviceId))
            {
                throw new InvalidOperationException($"Circular dependency detected involving '{serviceId}'.");
            }

            if (graph.TryGetValue(serviceId, out var dependencies))
            {
                foreach (var dependency in dependencies.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
                {
                    if (graph.ContainsKey(dependency))
                    {
                        Visit(dependency);
                    }
                }
            }

            visiting.Remove(serviceId);
            visited.Add(serviceId);
        }
    }

    public static IReadOnlyList<string> DetectDuplicatePorts(
        IEnumerable<(string ServiceId, int ContainerPort)> services)
    {
        var seen = new Dictionary<int, string>();
        var errors = new List<string>();

        foreach (var (serviceId, containerPort) in services.OrderBy(item => item.ServiceId, StringComparer.OrdinalIgnoreCase))
        {
            if (seen.TryGetValue(containerPort, out var existing))
            {
                errors.Add($"Duplicate container port {containerPort} declared by '{existing}' and '{serviceId}'.");
                continue;
            }

            seen[containerPort] = serviceId;
        }

        return errors;
    }

    public static IReadOnlyList<string> DetectMissingDependencies(
        IReadOnlyDictionary<string, IReadOnlyList<string>> graph,
        IReadOnlySet<string> knownServices)
    {
        var errors = new List<string>();

        foreach (var (serviceId, dependencies) in graph.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!knownServices.Contains(serviceId))
            {
                continue;
            }

            foreach (var dependency in dependencies.Where(dependency => !knownServices.Contains(dependency)))
            {
                errors.Add($"Service '{serviceId}' depends on unknown service '{dependency}'.");
            }
        }

        return errors;
    }

    private static void InsertSorted(List<string> values, string value)
    {
        var index = values.FindIndex(existing => string.CompareOrdinal(existing, value) > 0);
        if (index < 0)
        {
            values.Add(value);
            return;
        }

        values.Insert(index, value);
    }
}
