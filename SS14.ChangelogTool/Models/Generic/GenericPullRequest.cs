namespace SS14.ChangelogTool.Models.Generic;

// These can be used with both GitHub and Forgejo
public record GenericPullRequest(
    bool Merged,
    string? Body,
    GenericUser? Author,
    DateTimeOffset? MergedAt,
    GenericPullRequestBase? Base,
    int Number,
    string Url
);
