using SS14.ChangelogTool.Models.GitHub;

namespace SS14.ChangelogTool.Services;

/// <summary>
/// Service for aggregating pull requests into diffs.
/// </summary>
public interface IPullRequestService
{
    /// <summary>
    /// Gets the diff (new pull requests and reverted pull request numbers) since the provided commit hash.
    /// Repo is set by app settings.
    /// </summary>
    Task<GitHubDiff> GetDiff(string sinceSha);
}