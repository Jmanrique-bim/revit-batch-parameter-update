using BatchParamUpdate.Domain.ErrorCatalog;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class ErrorWarningCatalogTests
{
    [Fact]
    public void EveryErrorCode_HasSeverity500_AndOneNonTechnicalMessage()
    {
        foreach (var code in Enum.GetValues<ErrorCode>())
        {
            Assert.Equal(500, ErrorWarningCatalog.Severity(code));
            Assert.StartsWith("ERR-500-", ErrorWarningCatalog.Code(code));
            var message = ErrorWarningCatalog.Message(code);
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.DoesNotContain("API", message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EveryWarningCode_HasSeverity400_AndOneNonTechnicalMessage()
    {
        foreach (var code in Enum.GetValues<WarningCode>())
        {
            Assert.Equal(400, ErrorWarningCatalog.Severity(code));
            Assert.StartsWith("WARN-400-", ErrorWarningCatalog.Code(code));
            var message = ErrorWarningCatalog.Message(code);
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.DoesNotContain("API", message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CatalogMessages_MatchSpecLiterals()
    {
        Assert.Equal(
            "No elements are selected. Select one or more elements before continuing.",
            ErrorWarningCatalog.Message(ErrorCode.EmptySelection));
        Assert.Equal(
            "Choose a parameter from Dialog Box 1 or Dialog Box 2 before continuing.",
            ErrorWarningCatalog.Message(ErrorCode.NoParameterSelected));
        Assert.Equal(
            "Enter a parameter and a replacement value before running the update.",
            ErrorWarningCatalog.Message(ErrorCode.EmptyValue));
        Assert.Equal(
            "The model cannot be modified right now. No changes were made.",
            ErrorWarningCatalog.Message(ErrorCode.DocumentNotModifiable));
        Assert.Equal(
            "Open a model in Revit before running this tool.",
            ErrorWarningCatalog.Message(ErrorCode.NoActiveDocument));
        Assert.Equal(
            "This element does not have the selected parameter.",
            ErrorWarningCatalog.Message(WarningCode.ParamMissing));
        Assert.Equal(
            "This parameter cannot be edited on this element.",
            ErrorWarningCatalog.Message(WarningCode.ParamReadonly));
        Assert.Equal(
            "This parameter does not hold text and cannot be updated by this tool.",
            ErrorWarningCatalog.Message(WarningCode.ParamNotText));
        Assert.Equal(
            "This element is currently being edited by another user and was skipped.",
            ErrorWarningCatalog.Message(WarningCode.WorkshareOwned));
        Assert.Equal(
            "This element belongs to a group and cannot be batch-updated here. Edit it from within the group in Revit, or ungroup it, and try again.",
            ErrorWarningCatalog.Message(WarningCode.ModelGroupMember));
        Assert.Equal(
            "No parameters match your search.",
            ErrorWarningCatalog.Message(WarningCode.NoSearchMatch));
        Assert.Equal(
            "The session record could not be saved. The update still completed.",
            ErrorWarningCatalog.Message(WarningCode.SessionRecordFailed));
    }
}
