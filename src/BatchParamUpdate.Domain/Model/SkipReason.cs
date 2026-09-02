namespace BatchParamUpdate.Domain.Model;

public enum SkipReason
{
    ParameterMissing,
    ParameterReadOnly,
    ParameterNotText,
    WorksharingOwnedByOther,
    ModelGroupMember,
    ValueRejected,
    ElementNotFound,
    OtherSuppressedNativeDialog
}
