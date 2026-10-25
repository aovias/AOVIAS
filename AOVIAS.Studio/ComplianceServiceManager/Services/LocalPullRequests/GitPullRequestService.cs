using System.Globalization;
using System.Text;
using ComplianceServiceManager.Models.LocalPullRequests;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Services.LocalPullRequests;

/// <summary>Local-only Git PR operations via allowlisted <see cref="GitCliRunner"/>.</summary>
public sealed class GitPullRequestService : IGitPullRequestService
{
    private readonly GitCliRunner git;

    public GitPullRequestService(GitCliRunner git)
    {
        this.git = git;
    }

    public async Task<bool> BranchExistsAsync(string worktreePath, string branch, CancellationToken cancellationToken = default)
    {
        EnsureSafeRef(branch);
        var result = await git.RunAsync(worktreePath, $"show-ref --verify --quiet refs/heads/{Quote(branch)}", cancellationToken);
        if (result.Succeeded)
        {
            return true;
        }

        // Fallback without quiet for older parsing.
        var listed = await git.RunAsync(worktreePath, $"branch --list {Quote(branch)}", cancellationToken);
        return listed.Succeeded && listed.StandardOutput.Contains(branch, StringComparison.Ordinal);
    }

    public async Task<string?> GetTipShaAsync(string worktreePath, string branchOrRef, CancellationToken cancellationToken = default)
    {
        EnsureSafeRef(branchOrRef);
        var result = await git.RunAsync(worktreePath, $"rev-parse {Quote(branchOrRef)}", cancellationToken);
        if (!result.Succeeded)
        {
            return null;
        }

        var sha = result.StandardOutput.Trim();
        return string.IsNullOrWhiteSpace(sha) ? null : sha;
    }

    public async Task<string?> GetMergeBaseAsync(string worktreePath, string sourceBranch, string targetBranch, CancellationToken cancellationToken = default)
    {
        EnsureSafeRef(sourceBranch);
        EnsureSafeRef(targetBranch);
        var result = await git.RunAsync(
            worktreePath,
            $"merge-base {Quote(targetBranch)} {Quote(sourceBranch)}",
            cancellationToken);
        if (!result.Succeeded)
        {
            return null;
        }

        var sha = result.StandardOutput.Trim();
        return string.IsNullOrWhiteSpace(sha) ? null : sha;
    }

    public async Task<(int Ahead, int Behind)> GetAheadBehindAsync(
        string worktreePath,
        string sourceBranch,
        string targetBranch,
        CancellationToken cancellationToken = default)
    {
        EnsureSafeRef(sourceBranch);
        EnsureSafeRef(targetBranch);
        var result = await git.RunAsync(
            worktreePath,
            $"rev-list --left-right --count {Quote(targetBranch)}...{Quote(sourceBranch)}",
            cancellationToken);
        if (!result.Succeeded)
        {
            return (0, 0);
        }

        var parts = result.StandardOutput.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var behind)
            || !int.TryParse(parts[1], out var ahead))
        {
            return (0, 0);
        }

        // left = target, right = source → behind = commits on target not in source, ahead = source not in target
        return (ahead, behind);
    }

    public async Task<bool> IsWorkingTreeCleanAsync(string worktreePath, CancellationToken cancellationToken = default)
    {
        var result = await git.RunAsync(worktreePath, "status --porcelain", cancellationToken);
        return result.Succeeded && string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    public async Task<string?> GetCurrentBranchAsync(string worktreePath, CancellationToken cancellationToken = default)
    {
        var result = await git.RunAsync(worktreePath, "rev-parse --abbrev-ref HEAD", cancellationToken);
        if (!result.Succeeded)
        {
            return null;
        }

        var branch = result.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(branch) || string.Equals(branch, "HEAD", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return branch;
    }

    public async Task<IReadOnlyList<LocalPullRequestCommitInfo>> GetCommitsAsync(
        string worktreePath,
        string sourceBranch,
        string targetBranch,
        CancellationToken cancellationToken = default)
    {
        EnsureSafeRef(sourceBranch);
        EnsureSafeRef(targetBranch);
        var result = await git.RunAsync(
            worktreePath,
            $"log --format=%H%x1f%h%x1f%s%x1f%an%x1f%aI --reverse {Quote(targetBranch)}..{Quote(sourceBranch)}",
            cancellationToken);
        if (!result.Succeeded)
        {
            return [];
        }

        var commits = new List<LocalPullRequestCommitInfo>();
        foreach (var line in result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\u001f');
            if (parts.Length < 4)
            {
                continue;
            }

            DateTimeOffset? authored = null;
            if (parts.Length >= 5 && DateTimeOffset.TryParse(parts[4], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                authored = parsed;
            }

            commits.Add(new LocalPullRequestCommitInfo
            {
                Sha = parts[0],
                ShortSha = parts[1],
                Subject = parts[2],
                Author = parts[3],
                AuthoredAt = authored
            });
        }

        return commits;
    }

    public async Task<IReadOnlyList<LocalPullRequestFileChange>> GetChangedFilesAsync(
        string worktreePath,
        string sourceBranch,
        string targetBranch,
        CancellationToken cancellationToken = default)
    {
        EnsureSafeRef(sourceBranch);
        EnsureSafeRef(targetBranch);
        var result = await git.RunAsync(
            worktreePath,
            $"diff --name-status {Quote(targetBranch)}...{Quote(sourceBranch)}",
            cancellationToken);
        if (!result.Succeeded)
        {
            return [];
        }

        var files = new List<LocalPullRequestFileChange>();
        foreach (var line in result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var tab = line.IndexOf('\t');
            if (tab <= 0)
            {
                continue;
            }

            var status = line[..tab].Trim();
            var path = line[(tab + 1)..].Trim();
            if (status.StartsWith('R') || status.StartsWith('C'))
            {
                var parts = path.Split('\t');
                path = parts[^1];
            }

            files.Add(new LocalPullRequestFileChange
            {
                Path = path,
                Status = status
            });
        }

        // Enrich with numstat when available.
        var numstat = await git.RunAsync(
            worktreePath,
            $"diff --numstat {Quote(targetBranch)}...{Quote(sourceBranch)}",
            cancellationToken);
        if (numstat.Succeeded)
        {
            var byPath = files.ToDictionary(file => file.Path, StringComparer.OrdinalIgnoreCase);
            foreach (var line in numstat.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('\t');
                if (parts.Length < 3)
                {
                    continue;
                }

                var path = parts[^1];
                if (!byPath.TryGetValue(path, out var existing))
                {
                    continue;
                }

                _ = int.TryParse(parts[0], out var add);
                _ = int.TryParse(parts[1], out var del);
                byPath[path] = new LocalPullRequestFileChange
                {
                    Path = existing.Path,
                    Status = existing.Status,
                    Additions = add,
                    Deletions = del
                };
            }

            return byPath.Values.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase).ToList();
        }

        return files;
    }

    public async Task<(int Additions, int Deletions)> GetDiffStatAsync(
        string worktreePath,
        string sourceBranch,
        string targetBranch,
        CancellationToken cancellationToken = default)
    {
        var files = await GetChangedFilesAsync(worktreePath, sourceBranch, targetBranch, cancellationToken);
        return (files.Sum(file => file.Additions), files.Sum(file => file.Deletions));
    }

    public async Task<string> GetFileDiffAsync(
        string worktreePath,
        string sourceBranch,
        string targetBranch,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        EnsureSafeRef(sourceBranch);
        EnsureSafeRef(targetBranch);
        EnsureSafePath(relativePath);
        var result = await git.RunAsync(
            worktreePath,
            $"diff {Quote(targetBranch)}...{Quote(sourceBranch)} -- {Quote(relativePath)}",
            cancellationToken);
        if (!result.Succeeded)
        {
            return result.StandardError.Trim();
        }

        return string.IsNullOrWhiteSpace(result.StandardOutput)
            ? "(no textual diff)"
            : result.StandardOutput;
    }

    public async Task<(bool HasConflicts, string? Summary)> DetectMergeConflictsAsync(
        string worktreePath,
        string sourceBranch,
        string targetBranch,
        CancellationToken cancellationToken = default)
    {
        EnsureSafeRef(sourceBranch);
        EnsureSafeRef(targetBranch);
        var mergeBase = await GetMergeBaseAsync(worktreePath, sourceBranch, targetBranch, cancellationToken);
        if (string.IsNullOrWhiteSpace(mergeBase))
        {
            return (false, "Unable to determine merge-base.");
        }

        // Read-only conflict probe via merge-tree (does not modify worktree/index).
        var result = await git.RunAsync(
            worktreePath,
            $"merge-tree {Quote(mergeBase)} {Quote(targetBranch)} {Quote(sourceBranch)}",
            cancellationToken);
        if (!result.Succeeded && string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return (false, result.StandardError.Trim());
        }

        var output = result.StandardOutput ?? string.Empty;
        if (output.Contains("changed in both", StringComparison.OrdinalIgnoreCase)
            || output.Contains("CONFLICT", StringComparison.OrdinalIgnoreCase)
            || output.Contains("<<<<<<<", StringComparison.Ordinal))
        {
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Where(line =>
                    line.Contains("changed in both", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("CONFLICT", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("<<<<<<<", StringComparison.Ordinal))
                .Take(12)
                .ToList();
            return (true, lines.Count == 0 ? "Merge conflicts detected." : string.Join(Environment.NewLine, lines));
        }

        return (false, null);
    }

    public async Task<(bool Succeeded, string? MergeCommitSha, string? Error)> MergeNoFfAsync(
        string targetWorktreePath,
        string sourceBranch,
        string expectedTargetBranch,
        string message,
        CancellationToken cancellationToken = default)
    {
        EnsureSafeRef(sourceBranch);
        EnsureSafeRef(expectedTargetBranch);

        var current = await GetCurrentBranchAsync(targetWorktreePath, cancellationToken);
        if (!string.Equals(current, expectedTargetBranch, StringComparison.OrdinalIgnoreCase))
        {
            return (false, null,
                $"Target worktree is on '{current ?? "detached HEAD"}' but Local PR target is '{expectedTargetBranch}'. Check out the target branch explicitly before merge.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"Merge branch '{sourceBranch}'";
        }

        // Escape quotes in message for -m
        var safeMessage = message.Replace("\"", "\\\"");
        var result = await git.RunAsync(
            targetWorktreePath,
            $"merge --no-ff {Quote(sourceBranch)} -m \"{safeMessage}\"",
            cancellationToken);

        if (!result.Succeeded)
        {
            // Attempt to abort if merge left the repo in a conflicted state.
            _ = await git.RunAsync(targetWorktreePath, "merge --abort", cancellationToken);
            return (false, null, string.IsNullOrWhiteSpace(result.StandardError)
                ? result.StandardOutput.Trim()
                : result.StandardError.Trim());
        }

        var tip = await GetTipShaAsync(targetWorktreePath, "HEAD", cancellationToken);
        return (true, tip, null);
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

    private static void EnsureSafeRef(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Git ref is required.");
        }

        if (value.Any(ch => char.IsControl(ch) || ch is ';' or '|' or '&' or '>' or '<' or '`'))
        {
            throw new ArgumentException($"Unsafe Git ref rejected: {value}");
        }

        if (value.Contains("..", StringComparison.Ordinal) || value.StartsWith('-'))
        {
            throw new ArgumentException($"Unsafe Git ref rejected: {value}");
        }
    }

    private static void EnsureSafePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Path is required.");
        }

        if (relativePath.Contains("..", StringComparison.Ordinal)
            || relativePath.Any(ch => char.IsControl(ch) || ch is ';' or '|' or '&'))
        {
            throw new ArgumentException($"Unsafe path rejected: {relativePath}");
        }
    }
}
