namespace BatchParamUpdate.Domain.Model;

/// <summary>
/// Stable identity for a parameter, carried from discovery to the write so the write targets the
/// exact parameter the user picked — not a namesake. Adapter fills whichever field applies;
/// <see cref="Name"/> is the always-present fallback and the display label.
/// </summary>
public readonly record struct ParameterKey(int? BuiltInId, Guid? SharedGuid, string Name)
{
    public static ParameterKey ByName(string name) => new(null, null, name);
}
