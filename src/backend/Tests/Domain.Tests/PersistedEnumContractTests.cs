using System.Globalization;
using Domain.Enums;

namespace Domain.Tests;

/// <summary>
/// These enums are persisted. Their integer ordinals are written into the database
/// (and, for <see cref="EApprovalStage"/>, their names are too), so renumbering or
/// renaming a member silently reclassifies every existing row rather than failing loudly.
///
/// Each member is therefore pinned against a hard-coded literal — deliberately NOT
/// <c>nameof</c>, because a <c>nameof</c> reference follows a rename and would let the
/// very drift these tests exist to catch sail through.
/// </summary>
public sealed class PersistedEnumContractTests
{
    [Theory]
    [InlineData("Draft", 0)]
    [InlineData("Submitted", 1)]
    [InlineData("PendingManagerApproval", 2)]
    [InlineData("PendingFinanceApproval", 3)]
    [InlineData("PendingProcurementApproval", 4)]
    [InlineData("Approved", 5)]
    [InlineData("Rejected", 6)]
    [InlineData("Cancelled", 7)]
    public void Purchase_order_status_ordinals_are_the_database_contract(string name, int ordinal)
    {
        AssertPersistedMember<EPurchaseOrderStatus>(name, ordinal);
    }

    [Theory]
    [InlineData("Manager", 1)]
    [InlineData("Finance", 2)]
    [InlineData("Procurement", 3)]
    public void Approval_stage_ordinals_are_the_persisted_stage_order(string name, int ordinal)
    {
        AssertPersistedMember<EApprovalStage>(name, ordinal);
    }

    [Theory]
    [InlineData("Approve", 0)]
    [InlineData("Reject", 1)]
    public void Approval_action_ordinals_are_the_database_contract(string name, int ordinal)
    {
        AssertPersistedMember<EApprovalAction>(name, ordinal);
    }

    [Theory]
    [InlineData("Administrator", 1)]
    [InlineData("User", 2)]
    [InlineData("Manager", 3)]
    [InlineData("Viewer", 4)]
    public void Role_ordinals_are_the_database_contract(string name, int ordinal)
    {
        AssertPersistedMember<ERole>(name, ordinal);
    }

    [Theory]
    [InlineData("Create", 1)]
    [InlineData("Update", 2)]
    [InlineData("Delete", 3)]
    [InlineData("Read", 4)]
    [InlineData("BulkCreate", 5)]
    [InlineData("BulkUpdate", 6)]
    [InlineData("BulkDelete", 7)]
    [InlineData("Login", 10)]
    [InlineData("Logout", 11)]
    [InlineData("FailedLogin", 12)]
    [InlineData("SessionCreated", 13)]
    [InlineData("SessionExpired", 14)]
    [InlineData("SessionRefreshed", 15)]
    [InlineData("PasswordChanged", 16)]
    [InlineData("RoleAssigned", 20)]
    [InlineData("RoleRemoved", 21)]
    [InlineData("RoleCreated", 22)]
    [InlineData("RoleUpdated", 23)]
    [InlineData("RoleDeleted", 24)]
    [InlineData("PermissionGranted", 25)]
    [InlineData("PermissionRevoked", 26)]
    [InlineData("PermissionUpdated", 27)]
    [InlineData("AccessDenied", 28)]
    [InlineData("FileUpload", 30)]
    [InlineData("FileDownload", 31)]
    [InlineData("FileDelete", 32)]
    [InlineData("Export", 40)]
    [InlineData("Import", 41)]
    [InlineData("SettingsChanged", 50)]
    [InlineData("SystemEvent", 51)]
    [InlineData("JobExecuted", 52)]
    [InlineData("EmailSent", 53)]
    [InlineData("DataMigration", 54)]
    public void Audit_action_ordinals_are_the_database_contract(string name, int ordinal)
    {
        AssertPersistedMember<EAuditAction>(name, ordinal);
    }

    [Theory]
    [InlineData("Info", 0)]
    [InlineData("Warning", 1)]
    [InlineData("Error", 2)]
    [InlineData("Critical", 3)]
    public void Audit_severity_ordinals_are_the_database_contract(string name, int ordinal)
    {
        AssertPersistedMember<EAuditSeverity>(name, ordinal);
    }

    [Theory]
    [InlineData("Data", 0)]
    [InlineData("Authentication", 1)]
    [InlineData("AccessControl", 2)]
    [InlineData("FileOperation", 3)]
    [InlineData("DataTransfer", 4)]
    [InlineData("System", 5)]
    public void Audit_category_ordinals_are_the_database_contract(string name, int ordinal)
    {
        AssertPersistedMember<EAuditCategory>(name, ordinal);
    }

    [Theory]
    [InlineData("Screen", 1)]
    [InlineData("Api", 2)]
    public void Access_function_type_ordinals_are_the_database_contract(string name, int ordinal)
    {
        AssertPersistedMember<EAccessFunctionType>(name, ordinal);
    }

    [Theory]
    [InlineData("TITLE", 0)]
    [InlineData("USER_TYPE", 1)]
    [InlineData("VENDOR_CATEGORY", 2)]
    [InlineData("CATALOG_CATEGORY", 3)]
    [InlineData("UNIT_OF_MEASURE", 4)]
    [InlineData("DELIVERY_LOCATION", 5)]
    [InlineData("CURRENCY", 6)]
    public void Code_type_names_are_the_seeded_lookup_contract(string name, int ordinal)
    {
        AssertPersistedMember<ECodeType>(name, ordinal);
    }

    [Theory]
    [InlineData("MR", 0)]
    [InlineData("MRS", 1)]
    [InlineData("ADMIN", 2)]
    [InlineData("USER", 3)]
    [InlineData("IT_SERVICES", 4)]
    [InlineData("OFFICE_SUPPLIES", 5)]
    [InlineData("MAINTENANCE", 6)]
    [InlineData("CONSULTING", 7)]
    [InlineData("LOGISTICS", 8)]
    [InlineData("HARDWARE", 9)]
    [InlineData("SOFTWARE", 10)]
    [InlineData("FURNITURE", 11)]
    [InlineData("STATIONERY", 12)]
    [InlineData("CLEANING", 13)]
    [InlineData("EACH", 14)]
    [InlineData("BOX", 15)]
    [InlineData("PACK", 16)]
    [InlineData("SET", 17)]
    [InlineData("HOUR", 18)]
    [InlineData("MAIN_OFFICE", 19)]
    [InlineData("WAREHOUSE", 20)]
    [InlineData("BRANCH_OFFICE", 21)]
    [InlineData("SGD", 22)]
    [InlineData("USD", 23)]
    public void Code_name_values_are_the_seeded_lookup_contract(string name, int ordinal)
    {
        AssertPersistedMember<ECodeName>(name, ordinal);
    }

    [Theory]
    [InlineData("Draft", 0)]
    [InlineData("Submitted", 1)]
    [InlineData("UnderReview", 2)]
    [InlineData("Approved", 3)]
    [InlineData("Rejected", 4)]
    [InlineData("Completed", 5)]
    [InlineData("Cancelled", 6)]
    [InlineData("ReturnedForRevision", 7)]
    public void Workflow_state_names_are_the_persisted_workflow_contract(string name, int ordinal)
    {
        AssertPersistedMember<EWorkflowState>(name, ordinal);
    }

    /// <summary>
    /// Guards the shape of each persisted enum: the member count (so an addition is a
    /// conscious decision), the underlying width (a narrower type would truncate stored
    /// ordinals) and the absence of aliases (two names sharing an ordinal make the stored
    /// value ambiguous on read-back).
    /// </summary>
    [Theory]
    [InlineData(typeof(EPurchaseOrderStatus), 8)]
    [InlineData(typeof(EApprovalStage), 3)]
    [InlineData(typeof(EApprovalAction), 2)]
    [InlineData(typeof(ERole), 4)]
    [InlineData(typeof(EAuditAction), 33)]
    [InlineData(typeof(EAuditSeverity), 4)]
    [InlineData(typeof(EAuditCategory), 6)]
    [InlineData(typeof(EAccessFunctionType), 2)]
    [InlineData(typeof(ECodeType), 7)]
    [InlineData(typeof(ECodeName), 24)]
    [InlineData(typeof(EWorkflowState), 8)]
    public void Persisted_enum_shape_is_pinned(Type enumType, int expectedMemberCount)
    {
        var actualMemberCount = Enum.GetNames(enumType).Length;
        var ordinals = Enum.GetValues(enumType)
            .Cast<object>()
            .Select(value => Convert.ToInt64(value, CultureInfo.InvariantCulture))
            .OrderBy(ordinal => ordinal)
            .ToList();
        var distinctOrdinals = ordinals.Distinct().ToList();

        Assert.Equal(typeof(int), Enum.GetUnderlyingType(enumType));
        Assert.Equal(expectedMemberCount, actualMemberCount);
        Assert.Equal(distinctOrdinals, ordinals);
        Assert.False(enumType.IsDefined(typeof(FlagsAttribute), inherit: false));
    }

    /// <summary>
    /// Casting an arbitrary integer to an enum never throws, so every value read back from
    /// the database can be undefined. Code that trusts a stored ordinal without an
    /// <see cref="Enum.IsDefined{TEnum}(TEnum)"/> gate is trusting the database.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    [InlineData(99)]
    public void Undefined_status_ordinals_survive_the_cast_but_fail_is_defined(int storedOrdinal)
    {
        var status = (EPurchaseOrderStatus)storedOrdinal;

        Assert.False(Enum.IsDefined(status));
        Assert.Null(Enum.GetName(status));
    }

    private static void AssertPersistedMember<TEnum>(string name, int ordinal)
        where TEnum : struct, Enum
    {
        var parsedFromName = Enum.Parse<TEnum>(name, ignoreCase: false);
        var castFromOrdinal = (TEnum)Enum.ToObject(typeof(TEnum), ordinal);

        Assert.Equal(ordinal, Convert.ToInt32(parsedFromName, CultureInfo.InvariantCulture));
        Assert.Equal(name, Enum.GetName(castFromOrdinal));
        Assert.Equal(parsedFromName, castFromOrdinal);
        Assert.True(Enum.IsDefined(parsedFromName));
    }
}
