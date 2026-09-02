namespace BatchParamUpdate.Domain.ErrorCatalog;

public enum WarningCode
{
    ParamMissing,
    ParamReadonly,
    ParamNotText,
    WorkshareOwned,
    ModelGroupMember,
    ValueRejected,
    ElementNotFound,
    NoSearchMatch,
    SessionRecordFailed
}
