using Domain.Enums;
using Domain.Models;

namespace Domain.Tests;

/// <summary>
/// The purchase-order approval chain is spread across two enums that must stay in step:
/// <see cref="EApprovalStage"/> drives <c>PurchaseOrderApproval.StageOrder</c>, and the
/// controller maps the next pending stage onto a <c>Pending{Stage}Approval</c> member of
/// <see cref="EPurchaseOrderStatus"/> — a switch whose default arm throws at runtime.
/// These tests are reflection-driven so adding a stage without its status fails here
/// rather than in production.
/// </summary>
public sealed class ApprovalWorkflowEnumInvariantTests
{
    private const string PendingPrefix = "Pending";
    private const string ApprovalSuffix = "Approval";

    [Fact]
    public void Every_approval_stage_has_a_matching_pending_purchase_order_status()
    {
        var stagesWithoutStatus = new List<string>();

        foreach (var stage in Enum.GetNames<EApprovalStage>())
        {
            if (!Enum.TryParse<EPurchaseOrderStatus>(
                    PendingPrefix + stage + ApprovalSuffix,
                    ignoreCase: false,
                    out var status)
                || !Enum.IsDefined(status))
            {
                stagesWithoutStatus.Add(stage);
            }
        }

        Assert.Empty(stagesWithoutStatus);
    }

    [Fact]
    public void Every_pending_purchase_order_status_maps_back_to_a_declared_stage()
    {
        var orphanedStatuses = new List<string>();

        foreach (var status in Enum.GetNames<EPurchaseOrderStatus>())
        {
            if (!status.StartsWith(PendingPrefix, StringComparison.Ordinal)
                || !status.EndsWith(ApprovalSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            var stageName = status[PendingPrefix.Length..^ApprovalSuffix.Length];
            if (!Enum.TryParse<EApprovalStage>(stageName, ignoreCase: false, out var stage)
                || !Enum.IsDefined(stage))
            {
                orphanedStatuses.Add(status);
            }
        }

        Assert.Empty(orphanedStatuses);
    }

    [Fact]
    public void The_status_enum_carries_exactly_one_pending_member_per_approval_stage()
    {
        var expected = Enum.GetNames<EApprovalStage>()
            .Select(stage => PendingPrefix + stage + ApprovalSuffix)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var actual = Enum.GetNames<EPurchaseOrderStatus>()
            .Where(name => name.StartsWith(PendingPrefix, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// The controller advances the chain with <c>a.StageOrder &gt; current.StageOrder</c>,
    /// so the stage ordinals must be unique and must start above the CLR default of a
    /// freshly constructed <c>PurchaseOrderApproval</c>.
    /// </summary>
    [Fact]
    public void Stage_orders_are_unique_and_start_above_the_unassigned_default()
    {
        var stageOrders = Enum.GetValues<EApprovalStage>()
            .Select(stage => (int)stage)
            .ToList();
        var distinctStageOrders = stageOrders.Distinct().OrderBy(order => order).ToList();
        var nonPositive = stageOrders.Where(order => order <= 0).ToList();

        Assert.Equal(distinctStageOrders, stageOrders.OrderBy(order => order).ToList());
        Assert.Empty(nonPositive);
        Assert.Equal(0, new PurchaseOrderApproval().StageOrder);
    }

    /// <summary>
    /// Approvals are sorted by <c>StageOrder</c> while lists and dashboards sort by the
    /// status ordinal; the two orderings must agree or the UI shows a chain that appears
    /// to run backwards.
    /// </summary>
    [Fact]
    public void Pending_status_ordinals_ascend_in_the_same_order_as_the_stages()
    {
        var statusOrdinals = Enum.GetValues<EApprovalStage>()
            .OrderBy(stage => (int)stage)
            .Select(stage => (int)Enum.Parse<EPurchaseOrderStatus>(
                PendingPrefix + Enum.GetName(stage) + ApprovalSuffix,
                ignoreCase: false))
            .ToList();
        var strictlyAscending = statusOrdinals.Distinct().OrderBy(ordinal => ordinal).ToList();

        Assert.Equal(strictlyAscending, statusOrdinals);
    }

    [Theory]
    [InlineData(EApprovalStage.Manager, "Manager", 1)]
    [InlineData(EApprovalStage.Finance, "Finance", 2)]
    [InlineData(EApprovalStage.Procurement, "Procurement", 3)]
    public void Stage_persists_as_its_name_and_orders_by_its_ordinal(
        EApprovalStage stage,
        string persistedText,
        int stageOrder)
    {
        Assert.Equal(persistedText, stage.ToString());
        Assert.Equal(stageOrder, (int)stage);
        Assert.Equal(stage, Enum.Parse<EApprovalStage>(persistedText, ignoreCase: false));
    }

    /// <summary>
    /// <c>HasConversion&lt;string&gt;()</c> means EF writes the member name and parses it
    /// back case-sensitively. An unknown or differently-cased value is a hard failure on
    /// materialisation, not a silent fallback.
    /// </summary>
    [Fact]
    public void Unknown_or_recased_stage_text_is_rejected_on_read_back()
    {
        Assert.Throws<ArgumentException>(
            () => Enum.Parse<EApprovalStage>("Director", ignoreCase: false));
        Assert.False(Enum.TryParse<EApprovalStage>("manager", ignoreCase: false, out _));
        Assert.False(Enum.TryParse<EApprovalStage>("", ignoreCase: false, out _));
    }

    /// <summary>
    /// <see cref="Enum.Parse{TEnum}(string, bool)"/> honours comma-separated composition
    /// even on an enum without <see cref="FlagsAttribute"/>, so <c>"Manager,Finance"</c>
    /// ORs 1 and 2 into a perfectly valid <c>Procurement</c>. Pinned because it is the
    /// reason stage text must never be parsed straight from an untrusted caller.
    /// </summary>
    [Fact]
    public void Comma_separated_stage_text_composes_into_an_unrelated_but_defined_stage()
    {
        Assert.True(Enum.TryParse<EApprovalStage>(
            "Manager,Finance",
            ignoreCase: false,
            out var composed));
        Assert.Equal(EApprovalStage.Procurement, composed);
        Assert.True(Enum.IsDefined(composed));
    }

    /// <summary>
    /// A column that used to hold integers still parses, and an out-of-range integer parses
    /// into an undefined value without throwing — which is why every read must be gated on
    /// <see cref="Enum.IsDefined{TEnum}(TEnum)"/> rather than on <c>TryParse</c> alone.
    /// </summary>
    [Fact]
    public void Numeric_stage_text_parses_but_only_defined_ordinals_are_trustworthy()
    {
        Assert.True(Enum.TryParse<EApprovalStage>("2", ignoreCase: false, out var legacy));
        Assert.Equal(EApprovalStage.Finance, legacy);
        Assert.True(Enum.IsDefined(legacy));

        Assert.True(Enum.TryParse<EApprovalStage>("99", ignoreCase: false, out var undefined));
        Assert.False(Enum.IsDefined(undefined));
    }

    [Fact]
    public void Audit_severity_ordinals_escalate_so_threshold_filters_hold()
    {
        Assert.True(EAuditSeverity.Info < EAuditSeverity.Warning);
        Assert.True(EAuditSeverity.Warning < EAuditSeverity.Error);
        Assert.True(EAuditSeverity.Error < EAuditSeverity.Critical);
    }

    /// <summary>
    /// Zero is what an unset column, a default-constructed entity and a failed
    /// deserialisation all produce. Where zero lands is therefore a design decision, and
    /// it differs on purpose between the workflow enums and the security enums.
    /// </summary>
    [Fact]
    public void Zero_lands_on_a_harmless_value_only_where_the_domain_declares_one()
    {
        Assert.Equal(EPurchaseOrderStatus.Draft, default(EPurchaseOrderStatus));
        Assert.Equal(EWorkflowState.Draft, default(EWorkflowState));
        Assert.Equal(EAuditSeverity.Info, default(EAuditSeverity));
        Assert.Equal(EAuditCategory.Data, default(EAuditCategory));

        // Security- and workflow-critical enums start at 1 so an unset column is undefined
        // rather than silently granting a role, an audit action or an approval stage.
        Assert.False(Enum.IsDefined(default(ERole)));
        Assert.False(Enum.IsDefined(default(EAccessFunctionType)));
        Assert.False(Enum.IsDefined(default(EApprovalStage)));
        Assert.False(Enum.IsDefined(default(EAuditAction)));

        // EApprovalAction has no such guard — zero is Approve — which is exactly why the
        // entity models the decision as nullable, with null meaning "not decided yet".
        Assert.Equal(EApprovalAction.Approve, default(EApprovalAction));
        Assert.Null(new PurchaseOrderApproval().Action);
    }
}
