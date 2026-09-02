using BatchParamUpdate.Domain.ErrorCatalog;

namespace BatchParamUpdate.Domain.Model;

public sealed record ElementSkip(
    ElementRef Element,
    SkipReason Reason,
    WarningCode Code,
    string Message)
{
    public static ElementSkip Create(ElementRef element, SkipReason reason)
    {
        var code = ToWarningCode(reason);
        return new ElementSkip(element, reason, code, ErrorWarningCatalog.Message(code));
    }

    // ponytail: no dedicated 400 code for OtherSuppressedNativeDialog; reuse WORKSHARE-OWNED as the suppressed-dialog bucket.
    public static WarningCode ToWarningCode(SkipReason reason) => reason switch
    {
        SkipReason.ParameterMissing => WarningCode.ParamMissing,
        SkipReason.ParameterReadOnly => WarningCode.ParamReadonly,
        SkipReason.ParameterNotText => WarningCode.ParamNotText,
        SkipReason.WorksharingOwnedByOther => WarningCode.WorkshareOwned,
        SkipReason.ModelGroupMember => WarningCode.ModelGroupMember,
        SkipReason.ValueRejected => WarningCode.ValueRejected,
        SkipReason.ElementNotFound => WarningCode.ElementNotFound,
        SkipReason.OtherSuppressedNativeDialog => WarningCode.WorkshareOwned,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
    };
}
