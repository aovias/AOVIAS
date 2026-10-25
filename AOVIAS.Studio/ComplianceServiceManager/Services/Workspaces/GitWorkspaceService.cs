using System.IO;
using System.Text.RegularExpressions;
using ComplianceServiceManager.Models.Workspaces;

namespace ComplianceServiceManager.Services.Workspaces;

/// <summary>CLI-backed read-only Git inspection used by Local Development workspace awareness.</summary>
public sealed class GitWorkspaceService : IGitWorkspaceService
{
    private static readonly Regex PorcelainWorktreePath = new(@"^worktree (.+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PorcelainHead = new(@"^HEAD (.+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PorcelainBranch = new(@"^branch refs/heads/(.+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly GitCliRunner gitCliRunner;

    public GitWorkspaceService(GitCliRunner gitCliRunner)
    {
        this.gitCliRunner = gitCliRunner;
    }

    public bool LooksLikeGitWorktree(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        var gitMarker = Path.Combine(path, ".git");
        return File.Exists(gitMarker) || Directory.Exists(gitMarker);
    }

    public async Task<IReadOnlyList<GitWorktreeInfo>> ListWorktreesAsync(
        string repositoryOrWorktreePath,
        CancellationToken cancellationToken = default)
    {
        if (!LooksLikeGitWorktree(repositoryOrWorktreePath))
        {
            return Array.Empty<GitWorktreeInfo>();
        }

        var result = await gitCliRunner.RunAsync(
            repositoryOrWorktreePath,
            "worktree list --porcelain",
            cancellationToken);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(FormatGitError("worktree list", result));
        }

        return ParsePorcelainWorktrees(result.StandardOutput);
    }

    public async Task<GitWorkspaceState> GetWorkspaceStateAsync(
        string worktreePath,
        CancellationToken cancellationToken = default)
    {
        var observedAt = DateTimeOffset.UtcNow;
        if (string.IsNullOrWhiteSpace(worktreePath) || !Directory.Exists(worktreePath))
        {
            return Unavailable(worktreePath, "Workspace path is missing.", observedAt);
        }

        if (!LooksLikeGitWorktree(worktreePath))
        {
            return Unavailable(worktreePath, "Path is not a Git worktree (.git file or directory missing).", observedAt);
        }

        try
        {
            var branchResult = await gitCliRunner.RunAsync(worktreePath, "rev-parse --abbrev-ref HEAD", cancellationToken);
            if (!branchResult.Succeeded)
            {
                return Unavailable(worktreePath, FormatGitError("rev-parse --abbrev-ref HEAD", branchResult), observedAt);
            }

            var headResult = await gitCliRunner.RunAsync(worktreePath, "rev-parse HEAD", cancellationToken);
            if (!headResult.Succeeded)
            {
                return Unavailable(worktreePath, FormatGitError("rev-parse HEAD", headResult), observedAt);
            }

            var statusResult = await gitCliRunner.RunAsync(worktreePath, "status --porcelain", cancellationToken);
            if (!statusResult.Succeeded)
            {
                return Unavailable(worktreePath, FormatGitError("status --porcelain", statusResult), observedAt);
            }

            var branch = branchResult.StandardOutput.Trim();
            var isDetached = string.Equals(branch, "HEAD", StringComparison.OrdinalIgnoreCase);
            var head = headResult.StandardOutput.Trim();
            var isClean = string.IsNullOrWhiteSpace(statusResult.StandardOutput);

            int? ahead = null;
            int? behind = null;
            var upstreamResult = await gitCliRunner.RunAsync(
                worktreePath,
                "rev-list --left-right --count @{upstream}...HEAD",
                cancellationToken);
            if (upstreamResult.Succeeded)
            {
                var parts = upstreamResult.StandardOutput.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2
                    && int.TryParse(parts[0], out var behindCount)
                    && int.TryParse(parts[1], out var aheadCount))
                {
                    // rev-list --left-right --count A...B with @{upstream}...HEAD:
                    // left = commits reachable from upstream not in HEAD (behind)
                    // right = commits reachable from HEAD not in upstream (ahead)
                    behind = behindCount;
                    ahead = aheadCount;
                }
            }

            return new GitWorkspaceState
            {
                Path = Path.GetFullPath(worktreePath),
                IsAccessible = true,
                Branch = isDetached ? null : branch,
                Head = head,
                IsDetached = isDetached,
                IsClean = isClean,
                Ahead = ahead,
                Behind = behind,
                ObservedAt = observedAt
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unavailable(worktreePath, ex.Message, observedAt);
        }
        catch (DirectoryNotFoundException ex)
        {
            return Unavailable(worktreePath, ex.Message, observedAt);
        }
        catch (InvalidOperationException ex)
        {
            return Unavailable(worktreePath, ex.Message, observedAt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var message = ex is System.ComponentModel.Win32Exception
                ? "Git executable is unavailable."
                : $"Git repository cannot be accessed. ({ex.GetType().Name})";
            return Unavailable(worktreePath, message, observedAt);
        }
    }

    internal static IReadOnlyList<GitWorktreeInfo> ParsePorcelainWorktrees(string output)
    {
        var worktrees = new List<GitWorktreeInfo>();
        string? path = null;
        string? head = null;
        string? branch = null;
        var bare = false;
        var detached = false;

        void Flush()
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            worktrees.Add(new GitWorktreeInfo
            {
                Path = path,
                Head = head,
                Branch = branch,
                IsBare = bare,
                IsDetached = detached || string.IsNullOrWhiteSpace(branch)
            });

            path = null;
            head = null;
            branch = null;
            bare = false;
            detached = false;
        }

        foreach (var rawLine in output.Split(['\r', '\n'], StringSplitOptions.None))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                Flush();
                continue;
            }

            var line = rawLine.TrimEnd();
            var pathMatch = PorcelainWorktreePath.Match(line);
            if (pathMatch.Success)
            {
                Flush();
                path = pathMatch.Groups[1].Value.Trim();
                continue;
            }

            var headMatch = PorcelainHead.Match(line);
            if (headMatch.Success)
            {
                head = headMatch.Groups[1].Value.Trim();
                continue;
            }

            var branchMatch = PorcelainBranch.Match(line);
            if (branchMatch.Success)
            {
                branch = branchMatch.Groups[1].Value.Trim();
                continue;
            }

            if (line.Equals("bare", StringComparison.OrdinalIgnoreCase))
            {
                bare = true;
            }
            else if (line.Equals("detached", StringComparison.OrdinalIgnoreCase))
            {
                detached = true;
            }
        }

        Flush();
        return worktrees;
    }

    private static GitWorkspaceState Unavailable(string? path, string error, DateTimeOffset observedAt)
        => new()
        {
            Path = string.IsNullOrWhiteSpace(path) ? string.Empty : path,
            IsAccessible = false,
            IsClean = false,
            Error = error,
            ObservedAt = observedAt
        };

    private static string FormatGitError(string operation, GitCliResult result)
    {
        var detail = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput.Trim()
            : result.StandardError.Trim();

        if (string.IsNullOrWhiteSpace(detail))
        {
            return $"Git {operation} failed (exit {result.ExitCode}).";
        }

        // Keep logs/UI free of secrets: surface only the first line of stderr.
        var firstLine = detail.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? detail;
        return $"Git {operation} failed: {firstLine}";
    }
}
