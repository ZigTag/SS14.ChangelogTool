using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SS14.ChangelogTool.Models.Forgejo;
using SS14.ChangelogTool.Models.GitHub;
using SS14.ChangelogTool.Options;

namespace SS14.ChangelogTool.Clients;

/// <inheritdoc/>
public class ForgejoGitRepositoryClient(HttpClient client, INetworkGitRepositoryClient ghGraphQlClient, IOptions<ChangelogToolOptions> options) : INetworkGitRepositoryClient
{
    public readonly string ForgejoApiBase = $"https://{options.Value.Host}/api/v1";

    private static GitHubPullRequest ForgejoToGenericPull(ForgejoPullRequest fjPull)
    {
        return new GitHubPullRequest(fjPull.Merged, fjPull.Body, new GitHubUser(fjPull.User.Login), fjPull.MergedAt,
            new GitHubPullRequestBase(fjPull.Base.Ref), fjPull.Number, fjPull.Html_url);
    }
    
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<GitHubPullRequest>> GetPullRequests(
        string repo,
        IReadOnlyCollection<int> pullRequestNumbers
    )
    {
        if (pullRequestNumbers.Count == 0)
            return [];

        var (owner, repository) = ExtractParts(repo);

        var result = new List<GitHubPullRequest>();

        foreach (var prNumber in pullRequestNumbers)
        {
            var resp = await client.GetAsync($"{ForgejoApiBase}/repos/{owner}/{repository}/pulls/{prNumber}");

            if (!resp.IsSuccessStatusCode)
                continue;

            var contents = await resp.Content.ReadFromJsonAsync<ForgejoPullRequest>();
            
            if (contents is null)
                continue;
            
            result.Add(ForgejoToGenericPull(contents));
        }
        
        return result;
    }

    // Unfortunately it's hard to broker Forgejo for this, likely most external PRs will come off GitHub. 
    public async Task<IReadOnlyCollection<(string Sha, string RepoWithOwner)>> GetOwnedBy(IReadOnlyCollection<string> shaListToDiscover)
    {
        var ghPrs = (await ghGraphQlClient.GetOwnedBy(shaListToDiscover)).ToDictionary();

        // Assume that any PR that can't be found on GitHub is from the Forgejo repo
        return [.. shaListToDiscover.Where(val => !ghPrs.ContainsKey(val)).Select(val => (val, options.Value.Repo))];
    }

    private static (string repo, string owner) ExtractParts(string repo)
    {
        var parts = repo.Split('/', 2);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException(
                $"Attempted to split repo name {repo} into repository name and owner parts, "
                + $"but splitting by '/' resulted in {parts.Length} parts!"
            );
        }

        return (parts[0], parts[1]);
    }
}
