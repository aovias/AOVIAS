using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceServiceManager.Models.LocalPullRequests;

namespace ComplianceServiceManager.Services.LocalPullRequests;

/// <summary>Persists local PR metadata under %LocalAppData%\AoviasStudio\local-pull-requests.json.</summary>
public sealed class LocalPullRequestRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string storePath;
    private readonly object gate = new();
    private LocalPullRequestRegistryDocument document;

    public LocalPullRequestRegistry(string? storePath = null)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AoviasStudio");
        Directory.CreateDirectory(root);
        this.storePath = storePath ?? Path.Combine(root, "local-pull-requests.json");
        document = Load();
    }

    public string StorePath => storePath;

    public IReadOnlyList<LocalPullRequestDefinition> GetAll()
    {
        lock (gate)
        {
            return document.PullRequests.Select(Clone).ToList();
        }
    }

    public LocalPullRequestDefinition? GetById(string id)
    {
        lock (gate)
        {
            var match = document.PullRequests.FirstOrDefault(pr =>
                string.Equals(pr.Id, id, StringComparison.OrdinalIgnoreCase));
            return match is null ? null : Clone(match);
        }
    }

    public void Upsert(LocalPullRequestDefinition definition)
    {
        lock (gate)
        {
            var index = document.PullRequests.FindIndex(existing =>
                string.Equals(existing.Id, definition.Id, StringComparison.OrdinalIgnoreCase));
            var clone = Clone(definition);
            if (index >= 0)
            {
                document.PullRequests[index] = clone;
            }
            else
            {
                document.PullRequests.Add(clone);
            }

            Persist();
        }
    }

    public bool Remove(string id)
    {
        lock (gate)
        {
            var removed = document.PullRequests.RemoveAll(pr =>
                string.Equals(pr.Id, id, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
            {
                return false;
            }

            Persist();
            return true;
        }
    }

    private LocalPullRequestRegistryDocument Load()
    {
        try
        {
            if (File.Exists(storePath))
            {
                var loaded = JsonSerializer.Deserialize<LocalPullRequestRegistryDocument>(
                    File.ReadAllText(storePath),
                    JsonOptions);
                if (loaded is not null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
        }

        return new LocalPullRequestRegistryDocument();
    }

    private void Persist()
    {
        try
        {
            File.WriteAllText(storePath, JsonSerializer.Serialize(document, JsonOptions));
        }
        catch
        {
        }
    }

    private static LocalPullRequestDefinition Clone(LocalPullRequestDefinition source)
        => new()
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description,
            RepositoryId = source.RepositoryId,
            SourceWorktreePath = source.SourceWorktreePath,
            TargetWorktreePath = source.TargetWorktreePath,
            SourceBranch = source.SourceBranch,
            TargetBranch = source.TargetBranch,
            AuthorAgent = source.AuthorAgent,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            Status = source.Status,
            BlockReason = source.BlockReason,
            Validation = new LocalPullRequestValidation
            {
                Build = source.Validation.Build,
                Tests = source.Validation.Tests,
                Security = source.Validation.Security,
                CompletionCheck = source.Validation.CompletionCheck,
                Overall = source.Validation.Overall,
                Timestamp = source.Validation.Timestamp,
                Details = source.Validation.Details
            },
            LastReview = source.LastReview is null
                ? null
                : new LocalPullRequestReviewRecord
                {
                    Reviewer = source.LastReview.Reviewer,
                    Timestamp = source.LastReview.Timestamp,
                    Comment = source.LastReview.Comment,
                    SourceCommitSha = source.LastReview.SourceCommitSha,
                    TargetCommitSha = source.LastReview.TargetCommitSha,
                    IsApproval = source.LastReview.IsApproval
                },
            MergeCommitSha = source.MergeCommitSha,
            MergedAt = source.MergedAt
        };
}
