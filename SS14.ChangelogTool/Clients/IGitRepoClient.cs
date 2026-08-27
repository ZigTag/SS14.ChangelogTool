using SS14.ChangelogTool.Models.GitHub;

namespace SS14.ChangelogTool.Clients;

/// <summary>
/// Wrapper for extracting GitHub data through GraphQL API.
/// </summary>
public interface INetworkGitRepositoryClient
{
    /// <summary>
    /// Extracts pull requests that have merge date greater, then provided date.
    /// </summary>
    /// <param name="repo">Repo to inspect, includes both repository name and owner, as '{owner}\{repo}'.</param>
    /// <param name="pullRequestNumbers">List of pull request numbers that we should retrieve.</param>
    Task<IReadOnlyCollection<GitHubPullRequest>> GetPullRequests(string repo, IReadOnlyCollection<int> pullRequestNumbers);

    /// <summary>
    /// Extracts information about which in which repo was commit originally added.
    /// All squash-merged commits would be owned by respective repos.
    /// Uses batching.
    /// </summary>
    /// <param name="shaListToDiscover">List of SHA that we need detect owner repo for.</param>
    /// <returns>Pairs of SHA, Owner and RepoName (in format of 'owner/repo').</returns>
    Task<IReadOnlyCollection<(string Sha, string RepoWithOwner)>> GetOwnedBy(IReadOnlyCollection<string> shaListToDiscover);
}