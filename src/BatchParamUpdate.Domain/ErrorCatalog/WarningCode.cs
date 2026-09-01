namespace BatchParamUpdate.Domain.ErrorCatalog;

public enum WarningCode
{
    ParamMissing,
    ParamReadonly,
    ParamNotText,
    WorkshareOwned,
    ModelGroupMember,
    NoSearchMatch,
    SessionRecordFailed
}
