namespace Aovias.Deployment.Contracts.Capabilities;

/// <summary>The outcome of matching one requirement against the available capabilities (Sprint M2.3 D4).</summary>
public enum RequirementMatchStatus
{
    /// <summary>A capability was found and its version (if constrained) satisfies the requirement.</summary>
    Satisfied,

    /// <summary>No available capability's category (or accepted category) matches this requirement at all.</summary>
    Missing,

    /// <summary>A capability with a matching category exists, but its version does not satisfy the requirement's constraint.</summary>
    IncompatibleVersion
}

/// <summary>
/// The result of matching a single <see cref="Requirement"/>, including remediation guidance
/// for the two failure statuses (Sprint M2.3 D9).
/// </summary>
public sealed class RequirementMatchResult
{
    public required Requirement Requirement { get; init; }

    public required RequirementMatchStatus Status { get; init; }

    /// <summary>The capability that satisfied the requirement, or the best category match found despite an incompatible version.</summary>
    public Capability? MatchedCapability { get; init; }

    /// <summary>A deterministic, human-readable explanation — for <see cref="RequirementMatchStatus.Missing"/> or <see cref="RequirementMatchStatus.IncompatibleVersion"/>, includes suggested remediation.</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>Every requirement's match result plus the aggregate deployment decision (Sprint M2.3 D4/D5).</summary>
public sealed class RequirementMatchReport
{
    public required IReadOnlyList<RequirementMatchResult> Results { get; init; }

    /// <summary>False only when a <em>mandatory</em> requirement is <see cref="RequirementMatchStatus.Missing"/> or <see cref="RequirementMatchStatus.IncompatibleVersion"/>.</summary>
    public bool CanDeploy { get; init; }

    public IReadOnlyList<string> Blockers { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// The requirement-capability matching engine (Sprint M2.3 D4). Pure and deterministic: the same
/// requirements and the same available capabilities always produce the same report, in the same
/// order, which is what lets <see cref="Planning.DeploymentPlanner"/> and Runtime Agent/DRV treat
/// its output as an authoritative, replayable deployment decision rather than a live probe.
/// </summary>
public static class RequirementCapabilityMatcher
{
    public static RequirementMatchReport Evaluate(
        IReadOnlyList<Requirement> requirements,
        IReadOnlyList<Capability> availableCapabilities)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(availableCapabilities);

        var results = requirements.Select(requirement => EvaluateOne(requirement, availableCapabilities)).ToList();

        var blockers = results
            .Where(result => result.Requirement.Mandatory && result.Status != RequirementMatchStatus.Satisfied)
            .Select(result => result.Message)
            .ToList();

        var warnings = results
            .Where(result => !result.Requirement.Mandatory && result.Status != RequirementMatchStatus.Satisfied)
            .Select(result => result.Message)
            .ToList();

        return new RequirementMatchReport
        {
            Results = results,
            CanDeploy = blockers.Count == 0,
            Blockers = blockers,
            Warnings = warnings
        };
    }

    private static RequirementMatchResult EvaluateOne(Requirement requirement, IReadOnlyList<Capability> availableCapabilities)
    {
        var acceptedCategories = new[] { requirement.Category }
            .Concat(requirement.Compatibility.AcceptedCategories)
            .ToList();

        var candidates = availableCapabilities
            .Where(capability => acceptedCategories.Any(category =>
                string.Equals(capability.Category, category, StringComparison.OrdinalIgnoreCase)
                || capability.SatisfiesCategories.Any(satisfied => string.Equals(satisfied, category, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        if (candidates.Count == 0)
        {
            var suggestion = CapabilityRegistry.FindByCategory(requirement.Category).Select(c => c.Id).ToList();
            var remediation = suggestion.Count > 0
                ? $" Suggested remediation: add capability {string.Join(" or ", suggestion)} to this environment's declared profile."
                : " No registered capability satisfies this category at all — check CapabilityRegistry for a typo in the requirement's category.";

            return new RequirementMatchResult
            {
                Requirement = requirement,
                Status = RequirementMatchStatus.Missing,
                Message = $"Requirement '{requirement.Id}' ({requirement.Category}) has no matching capability in this environment.{remediation}"
            };
        }

        var versionCompatible = candidates.FirstOrDefault(
            capability => VersionConstraint.IsSatisfiedBy(requirement.VersionConstraint, capability.Version));

        if (versionCompatible is not null)
        {
            return new RequirementMatchResult
            {
                Requirement = requirement,
                Status = RequirementMatchStatus.Satisfied,
                MatchedCapability = versionCompatible,
                Message = $"Requirement '{requirement.Id}' satisfied by capability '{versionCompatible.Id}' ({versionCompatible.Version})."
            };
        }

        var closest = candidates[0];
        return new RequirementMatchResult
        {
            Requirement = requirement,
            Status = RequirementMatchStatus.IncompatibleVersion,
            MatchedCapability = closest,
            Message = $"Requirement '{requirement.Id}' ({requirement.Category}) found capability '{closest.Id}' at version {closest.Version}, " +
                      $"which does not satisfy constraint '{requirement.VersionConstraint}'. Suggested remediation: upgrade '{closest.Id}' " +
                      $"or relax the requirement's version constraint."
        };
    }
}
