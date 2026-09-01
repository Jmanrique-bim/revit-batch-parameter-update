using System.Diagnostics;

namespace BatchParamUpdate.Core;

public sealed class PhaseTimer : IDisposable
{
    private readonly Stopwatch _watch = Stopwatch.StartNew();

    public long ElapsedMs => _watch.ElapsedMilliseconds;

    public static PhaseTimer Start() => new();

    public void Dispose() => _watch.Stop();
}
