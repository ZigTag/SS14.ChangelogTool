namespace SS14.ChangelogTool.Models.Forgejo;

public sealed record ForgejoPullRequest(
    bool Merged,
    string Body,
    ForgejoUser User,
    DateTimeOffset? MergedAt,
    ForgejoPullRequestBase Base,
    int Number,
    string Html_url
);
