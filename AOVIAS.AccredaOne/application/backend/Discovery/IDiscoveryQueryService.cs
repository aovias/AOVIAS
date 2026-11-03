using AccredaOne.Api.DevelopmentSynthetic;
using AccredaOne.Api.Discovery.Contracts;

namespace AccredaOne.Api.Discovery;

public interface IDiscoveryQueryService
{
    DiscoveryQueryResponse Execute(DiscoveryQueryRequest request);
}

public sealed class DiscoveryQueryService(IDevelopmentSyntheticDataStore dataStore) : IDiscoveryQueryService
{
    public DiscoveryQueryResponse Execute(DiscoveryQueryRequest request)
    {
        var dataset = dataStore.GetDataset();
        var resolution = DevelopmentSyntheticQueryResolver.Resolve(request.FrameworkCode, dataset);

        if (string.Equals(resolution.Outcome, DevelopmentSyntheticQueryResolver.OutcomeNoResults, StringComparison.Ordinal))
        {
            return new DiscoveryQueryResponse
            {
                Outcome = resolution.Outcome,
                DatasetPackId = dataset.Manifest.PackId,
                DatasetClassification = dataset.Manifest.Classification,
            };
        }

        var frameworkById = dataset.Frameworks.ToDictionary(f => f.Id, StringComparer.Ordinal);
        var sourceById = dataset.Sources.ToDictionary(s => s.Id, StringComparer.Ordinal);
        var explanationByFrameworkId = dataset.Explanations.ToDictionary(e => e.FrameworkId, StringComparer.Ordinal);

        var orderedFrameworkIds = resolution.FrameworkIds
            .Select(id => frameworkById[id])
            .OrderBy(f => f.Code, StringComparer.Ordinal)
            .Select(f => f.Id)
            .ToList();

        var groups = new List<DiscoveryFrameworkGroupResponse>();
        var flatIds = new List<string>();

        foreach (var frameworkId in orderedFrameworkIds)
        {
            var framework = frameworkById[frameworkId];
            var requirements = DevelopmentSyntheticQueryResolver.GetCurrentRequirementsForFramework(dataset, frameworkId);
            var source = sourceById[requirements[0].SourceInstrumentId];
            explanationByFrameworkId.TryGetValue(frameworkId, out var explanation);

            var requirementResponses = requirements.Select(r => new DiscoveryRequirementResponse
            {
                Id = r.Id,
                Label = r.Label,
                Provenance = r.Provenance,
                HierarchyContext = r.HierarchyContext,
                SourceInstrumentId = r.SourceInstrumentId,
            }).ToList();

            flatIds.AddRange(requirementResponses.Select(r => r.Id));

            groups.Add(new DiscoveryFrameworkGroupResponse
            {
                FrameworkId = framework.Id,
                FrameworkCode = framework.Code,
                DisplayName = framework.DisplayName,
                FrameworkProvenance = framework.Provenance,
                SourceInstrument = new DiscoverySourceInstrumentResponse
                {
                    Id = source.Id,
                    Title = source.Title,
                    Provenance = source.Provenance,
                    ContentKind = source.ContentKind,
                },
                AccredaOneExplanation = explanation is null
                    ? null
                    : new DiscoveryAccredaOneExplanationResponse
                    {
                        Id = explanation.Id,
                        Title = explanation.Title,
                        Body = explanation.Body,
                        Provenance = explanation.Provenance,
                        ContentKind = explanation.ContentKind,
                    },
                Requirements = requirementResponses,
            });
        }

        return new DiscoveryQueryResponse
        {
            Outcome = resolution.Outcome,
            DatasetPackId = dataset.Manifest.PackId,
            DatasetClassification = dataset.Manifest.Classification,
            FrameworkGroups = groups,
            RequirementIds = flatIds,
        };
    }
}
