namespace BatchParamUpdate.Core;

public static class RunIdGenerator
{
    public static string NewRunId() => Guid.NewGuid().ToString("N")[..8];
}
