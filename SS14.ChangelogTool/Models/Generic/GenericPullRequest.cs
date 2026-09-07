namespace SS14.ChangelogTool.Models.Generic;

// These can be used with both GitHub and Forgejo
public sealed record GenericPullRequest(
    bool Merged,
    string? Merge_commit_sha,
    string? Body,
    GenericUser? Author,
    DateTimeOffset? MergedAt,
    GenericPullRequestBase? Base,
    int Number,
    string Url
);
