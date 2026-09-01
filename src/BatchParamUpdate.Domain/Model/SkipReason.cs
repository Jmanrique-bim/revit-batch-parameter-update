namespace BatchParamUpdate.Domain.Model;

public enum SkipReason
{
    ParameterMissing,
    ParameterReadOnly,
    ParameterNotText,
    WorksharingOwnedByOther,
    ModelGroupMember,
    OtherSuppressedNativeDialog
}
