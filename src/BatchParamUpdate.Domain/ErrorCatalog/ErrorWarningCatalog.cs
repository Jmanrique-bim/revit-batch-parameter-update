namespace BatchParamUpdate.Domain.ErrorCatalog;

public static class ErrorWarningCatalog
{
    public static int Severity(ErrorCode code) => 500;

    public static int Severity(WarningCode code) => 400;

    public static string Code(ErrorCode code) => code switch
    {
        ErrorCode.EmptySelection => "ERR-500-EMPTY-SELECTION",
        ErrorCode.NoParameterSelected => "ERR-500-NO-PARAMETER-SELECTED",
        ErrorCode.EmptyValue => "ERR-500-EMPTY-VALUE",
        ErrorCode.DocumentNotModifiable => "ERR-500-DOCUMENT-NOT-MODIFIABLE",
        ErrorCode.NoActiveDocument => "ERR-500-NO-ACTIVE-DOCUMENT",
        ErrorCode.BatchRolledBack => "ERR-500-BATCH-ROLLED-BACK",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };

    public static string Code(WarningCode code) => code switch
    {
        WarningCode.ParamMissing => "WARN-400-PARAM-MISSING",
        WarningCode.ParamReadonly => "WARN-400-PARAM-READONLY",
        WarningCode.ParamNotText => "WARN-400-PARAM-NOT-TEXT",
        WarningCode.WorkshareOwned => "WARN-400-WORKSHARE-OWNED",
        WarningCode.ModelGroupMember => "WARN-400-MODEL-GROUP-MEMBER",
        WarningCode.ValueRejected => "WARN-400-VALUE-REJECTED",
        WarningCode.ElementNotFound => "WARN-400-ELEMENT-NOT-FOUND",
        WarningCode.NoSearchMatch => "WARN-400-NO-SEARCH-MATCH",
        WarningCode.SessionRecordFailed => "WARN-400-SESSION-RECORD-FAILED",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };

    public static string Message(ErrorCode code) => code switch
    {
        ErrorCode.EmptySelection =>
            "No elements are selected. Select one or more elements before continuing.",
        ErrorCode.NoParameterSelected =>
            "Choose a parameter before continuing.",
        ErrorCode.EmptyValue =>
            "Enter a parameter and a replacement value before running the update.",
        ErrorCode.DocumentNotModifiable =>
            "The model cannot be modified right now. No changes were made.",
        ErrorCode.NoActiveDocument =>
            "Open a model in Revit before running this tool.",
        ErrorCode.BatchRolledBack =>
            "Revit rejected the changes. No elements were modified.",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };

    public static string Message(WarningCode code) => code switch
    {
        WarningCode.ParamMissing =>
            "This element does not have the selected parameter.",
        WarningCode.ParamReadonly =>
            "This parameter cannot be edited on this element.",
        WarningCode.ParamNotText =>
            "This parameter does not hold text and cannot be updated by this tool.",
        WarningCode.WorkshareOwned =>
            "This element is currently being edited by another user and was skipped.",
        WarningCode.ModelGroupMember =>
            "This element belongs to a group and cannot be batch-updated here. Edit it from within the group in Revit, or ungroup it, and try again.",
        WarningCode.ValueRejected =>
            "Revit did not accept the new value for this element.",
        WarningCode.ElementNotFound =>
            "This element no longer exists in the model.",
        WarningCode.NoSearchMatch =>
            "No parameters match your search.",
        WarningCode.SessionRecordFailed =>
            "The session record could not be saved. The update still completed.",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };
}
