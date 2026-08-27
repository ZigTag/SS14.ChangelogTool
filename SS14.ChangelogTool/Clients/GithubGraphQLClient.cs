using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Options;
using SS14.ChangelogTool.Models.GitHub;
using SS14.ChangelogTool.Options;

namespace SS14.ChangelogTool.Clients;

/// <inheritdoc/>
public class GithubGraphQLClient(IGraphQLClient graphQlClient, IOptions<ChangelogToolOptions> options) : INetworkGitRepositoryClient
{
    public const string GithubGraphQLApiBase = "https://api.github.com/graphql";

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<GitHubPullRequest>> GetPullRequests(
        string repo,
        IReadOnlyCollection<int> pullRequestNumbers
    )
    {
        if (pullRequestNumbers.Count == 0)
            return [];

        var (owner, repository) = ExtractParts(repo);

        var batchSize = options.Value.MaxPullRequestEntriesInGraphQLRequest;

        var result = new List<GitHubPullRequest>();

        var prNumberChunk = pullRequestNumbers.Distinct()
                                              .Chunk(batchSize);
        foreach (var batch in prNumberChunk)
        {
            var pullRequestFields = string.Join(
                "\n",
                batch.Select(number => $$"""
                                         pr{{number}}: pullRequest(number: {{number}}) {
                                           merged
                                           body
                                           author {
                                             login
                                           }
                                           mergedAt
                                           baseRef {
                                             name
                                           }
                                           number
                                           url
                                         }
                                         """
                )
            );

            var query = $$"""
                          {
                            repository(owner: "{{owner}}", name: "{{repository}}") {
                              {{pullRequestFields}}
                            }
                          }
                          """;

            var request = new GraphQLRequest(query);

            var response = await graphQlClient.SendQueryAsync<GitHubPullRequestsResponse>(request);

            EnsureSuccessful(response);

            result.AddRange(response.Data.Repository.Values.Where(x => x is not null)!);
        }

        return result;
    }

    public async Task<IReadOnlyCollection<(string Sha, string RepoWithOwner)>> GetOwnedBy(IReadOnlyCollection<string> shaListToDiscover)
    {
        if (shaListToDiscover.Count == 0)
            return [];

        var chunkSize = options.Value.MaxCommitEntriesInGraphQLRequest;
        var chunks = shaListToDiscover.Distinct()
                                      .Chunk(chunkSize);

        var result = new List<(string Sha, string RepoWithOwner)>();

        foreach (var chunk in chunks) 
        {
            var queryParts = new List<string>();
            for (int i = 0; i < chunk.Length; i++)
            {
                queryParts.Add(
                    $$"""
                      commit_{{i}}: search(query: "{{chunk[i]}} is:pr is:merged", type: ISSUE, first: 1) {
                          nodes {
                              ... on PullRequest {
                                  url
                                  repository {
                                      nameWithOwner
                                  }
                              }
                          }
                      }
                      """
                    );
            }
            var query = "query {" + string.Join("", queryParts) + "}";

            var request = new GraphQLRequest(query);

            var response = await graphQlClient.SendQueryAsync<Dictionary<string, CommitSearchNode>>(request);

            EnsureSuccessful(response);

            // The response contains one field per search, aliased as commit_{i}. Look each chunk entry up by its own
            // alias so SHAs stay correctly paired even when a search returns no matching pull requests.
            for (var i = 0; i < chunk.Length; i++)
            {
                if (!response.Data.TryGetValue($"commit_{i}", out var searchResult))
                    continue;

                var pullRequest = searchResult.Nodes.FirstOrDefault();
                if (pullRequest is null)
                    continue;

                result.Add((chunk[i], pullRequest.Repository.NameWithOwner));
            }
        }

        return result;
    }

    private static void EnsureSuccessful<T>(GraphQLResponse<T> response)
    {
        if (response.Errors is { Length: > 0 })
        {
            throw new InvalidOperationException(
                "GitHub GraphQL search failed when discovering commit owners: "
                + string.Join("; ", response.Errors.Select(e => e.Message))
            );
        }
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