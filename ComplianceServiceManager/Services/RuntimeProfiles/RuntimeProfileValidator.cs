using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.RuntimeProfiles;

namespace ComplianceServiceManager.Services.RuntimeProfiles;

/// <summary>
/// Validates profile membership/dependencies and produces a deterministic topological startup order.
/// </summary>
public sealed class RuntimeProfileValidator
{
    public ProfileValidationResult Validate(
        RuntimeProfileDefinition profile,
        IReadOnlyCollection<ServiceInfoModel>? discoveredServices = null)
    {
        var issues = new List<ProfileValidationIssue>();
        if (profile is null)
        {
            return ProfileValidationResult.Invalid(new ProfileValidationIssue
            {
                Code = "null-profile",
                Message = "Profile is null."
            });
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            issues.Add(new ProfileValidationIssue
            {
                Code = "missing-name",
                Message = "Profile name is required."
            });
        }

        var enabled = profile.Services
            .Where(entry => entry.Enabled)
            .ToList();

        if (enabled.Count == 0)
        {
            issues.Add(new ProfileValidationIssue
            {
                Code = "no-services",
                Message = "Profile has no enabled services."
            });
            return ProfileValidationResult.Invalid(issues);
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in enabled)
        {
            if (string.IsNullOrWhiteSpace(entry.ServiceName))
            {
                issues.Add(new ProfileValidationIssue
                {
                    Code = "empty-service",
                    Message = "Profile contains an empty service name."
                });
                continue;
            }

            if (!names.Add(entry.ServiceName))
            {
                issues.Add(new ProfileValidationIssue
                {
                    Code = "duplicate-service",
                    Message = $"Duplicate service '{entry.ServiceName}'."
                });
            }
        }

        var discoveredNames = discoveredServices is null
            ? null
            : new HashSet<string>(
                discoveredServices.Select(service => service.Name),
                StringComparer.OrdinalIgnoreCase);

        if (discoveredNames is not null)
        {
            foreach (var entry in enabled.Where(item => !string.IsNullOrWhiteSpace(item.ServiceName)))
            {
                if (!discoveredNames.Contains(entry.ServiceName))
                {
                    issues.Add(new ProfileValidationIssue
                    {
                        Code = "unknown-service",
                        Message = $"Unknown service '{entry.ServiceName}' (not in current discovery)."
                    });
                }
            }
        }

        foreach (var entry in enabled)
        {
            foreach (var dependency in entry.DependsOn.Where(name => !string.IsNullOrWhiteSpace(name)))
            {
                if (!names.Contains(dependency))
                {
                    var disabledMatch = profile.Services.FirstOrDefault(candidate =>
                        string.Equals(candidate.ServiceName, dependency, StringComparison.OrdinalIgnoreCase));
                    if (disabledMatch is { Enabled: false })
                    {
                        issues.Add(new ProfileValidationIssue
                        {
                            Code = "disabled-dependency",
                            Message = $"Service '{entry.ServiceName}' depends on disabled service '{dependency}'."
                        });
                    }
                    else
                    {
                        issues.Add(new ProfileValidationIssue
                        {
                            Code = "missing-dependency",
                            Message = $"Service '{entry.ServiceName}' depends on missing service '{dependency}'."
                        });
                    }
                }
            }
        }

        if (issues.Count > 0)
        {
            return ProfileValidationResult.Invalid(issues);
        }

        var (order, cycle) = TryTopologicalSort(enabled);
        if (cycle is not null)
        {
            return ProfileValidationResult.Invalid(new ProfileValidationIssue
            {
                Code = "circular-dependency",
                Message = $"Circular dependency detected involving '{cycle}'."
            });
        }

        return ProfileValidationResult.Valid(order);
    }

    /// <summary>Kahn topological sort; returns cycle witness service name when cyclic.</summary>
    internal static (IReadOnlyList<string> Order, string? CycleWitness) TryTopologicalSort(
        IReadOnlyList<RuntimeProfileServiceEntry> enabled)
    {
        var nodes = enabled
            .Select(entry => entry.ServiceName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var incoming = nodes.ToDictionary(name => name, _ => 0, StringComparer.OrdinalIgnoreCase);
        var adjacency = nodes.ToDictionary(
            name => name,
            _ => new List<string>(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var entry in enabled)
        {
            foreach (var dependency in entry.DependsOn
                         .Where(name => !string.IsNullOrWhiteSpace(name))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!adjacency.ContainsKey(dependency))
                {
                    continue;
                }

                adjacency[dependency].Add(entry.ServiceName);
                incoming[entry.ServiceName]++;
            }
        }

        var queue = new Queue<string>(
            incoming.Where(pair => pair.Value == 0)
                .Select(pair => pair.Key)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));

        var order = new List<string>();
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            order.Add(current);
            foreach (var next in adjacency[current].OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                incoming[next]--;
                if (incoming[next] == 0)
                {
                    queue.Enqueue(next);
                }
            }
        }

        if (order.Count != nodes.Count)
        {
            var witness = incoming
                .Where(pair => pair.Value > 0)
                .Select(pair => pair.Key)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? "unknown";
            return ([], witness);
        }

        return (order, null);
    }
}
