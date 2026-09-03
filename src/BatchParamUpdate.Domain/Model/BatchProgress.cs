namespace BatchParamUpdate.Domain.Model;

/// <summary>How far a running batch has got. <see cref="Done"/> of <see cref="Total"/> elements.</summary>
public readonly record struct BatchProgress(int Done, int Total)
{
    public double Fraction => Total <= 0 ? 0 : (double)Done / Total;
}
