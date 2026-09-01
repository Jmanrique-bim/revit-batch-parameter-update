namespace BatchParamUpdate.Domain.Model;

public sealed record SessionRecord(string RunId, string DocumentName, DateTimeOffset StartedAtUtc)
{
    public string SessionId => $"revit-{RunId}-{DocumentName}";
}
