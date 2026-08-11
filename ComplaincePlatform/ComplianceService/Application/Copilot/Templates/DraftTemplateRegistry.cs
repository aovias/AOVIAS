using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Templates;

/// <summary>
/// Resolves the correct <see cref="IDraftTemplate"/> for a given <see cref="DraftType"/>.
/// Templates are registered via DI and injected as a collection.
/// </summary>
public sealed class DraftTemplateRegistry
{
    private readonly IReadOnlyDictionary<DraftType, IDraftTemplate> _templates;

    public DraftTemplateRegistry(IEnumerable<IDraftTemplate> templates)
    {
        _templates = templates.ToDictionary(t => t.DraftType);
    }

    /// <summary>Returns the template for the given type, or throws if not registered.</summary>
    public IDraftTemplate Resolve(DraftType draftType)
    {
        if (_templates.TryGetValue(draftType, out var template))
            return template;

        throw new InvalidOperationException(
            $"No draft template registered for DraftType '{draftType}'. " +
            $"Available types: {string.Join(", ", _templates.Keys)}");
    }

    /// <summary>Returns true if a template is registered for the given type.</summary>
    public bool HasTemplate(DraftType draftType) => _templates.ContainsKey(draftType);

    /// <summary>Returns all registered templates.</summary>
    public IReadOnlyCollection<IDraftTemplate> All => _templates.Values.ToList();
}
