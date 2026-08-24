using Api.Controllers;
using Api.Tests.TestSupport;
using Application.Contracts;
using Application.Features.FileStorage;
using Application.Features.PurchaseOrder;
using Application.Features.PurchaseOrderDocument;
using Application.Integration;
using Contracts.Events.Procurement;
using Contracts.Integration;
using Domain.Enums;
using Domain.Models;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Api.Tests.Controllers;

/// <summary>
/// The purchase-order approval chain is the only real state machine in the host, and
/// every transition is guarded by a status check that must not be bypassable.
/// </summary>
public sealed class PurchaseOrderControllerTests
{
    private readonly IPurchaseOrderService _orders = Substitute.For<IPurchaseOrderService>();
    private readonly IPurchaseOrderDocumentService _documents =
        Substitute.For<IPurchaseOrderDocumentService>();
    private readonly IFileStorageService _storage = Substitute.For<IFileStorageService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IIntegrationEventPublisher _publisher =
        Substitute.For<IIntegrationEventPublisher>();

    public PurchaseOrderControllerTests()
    {
        _mapper.Map<PurchaseOrderDto>(Arg.Any<object>()).Returns(_ => new PurchaseOrderDto());
    }

    [Fact]
    public async Task Submitting_a_draft_builds_the_full_three_stage_approval_chain()
    {
        var order = Draft();
        Stub(order);

        await CreateController().Submit(order.Id, TestContext.Current.CancellationToken);

        Assert.Equal(EPurchaseOrderStatus.PendingManagerApproval, order.Status);
        Assert.Equal(
            new[] { EApprovalStage.Manager, EApprovalStage.Finance, EApprovalStage.Procurement },
            order.Approvals
                .OrderBy(approval => approval.StageOrder)
                .Select(approval => approval.ApprovalStage)
                .ToArray());
        Assert.All(order.Approvals, approval => Assert.Equal((int)approval.ApprovalStage, approval.StageOrder));
        Assert.All(order.Approvals, approval => Assert.Null(approval.Action));
    }

    [Theory]
    [InlineData(EPurchaseOrderStatus.PendingManagerApproval)]
    [InlineData(EPurchaseOrderStatus.Approved)]
    [InlineData(EPurchaseOrderStatus.Rejected)]
    [InlineData(EPurchaseOrderStatus.Cancelled)]
    public async Task Only_a_draft_can_be_submitted(EPurchaseOrderStatus status)
    {
        var order = Draft();
        order.Status = status;
        Stub(order);

        var result = await CreateController().Submit(order.Id, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(order.Approvals);
        await _orders.DidNotReceive().SaveOrUpdateAsync(Arg.Any<PurchaseOrder>());
    }

    [Fact]
    public async Task Submitting_an_unknown_order_is_a_404()
    {
        _orders
            .GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((PurchaseOrder?)null);

        var result = await CreateController().Submit(
            Guid.CreateVersion7(),
            TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Submission_publishes_the_status_transition_for_downstream_services()
    {
        var order = Draft();
        order.TotalAmount = 1234.56m;
        Stub(order);

        await CreateController().Submit(order.Id, TestContext.Current.CancellationToken);

        await _publisher.Received(1).EnqueueAsync(
            IntegrationContractCatalog.PurchaseOrderStatusChanged,
            Arg.Is<PurchaseOrderStatusChangedV1>(payload =>
                payload != null &&
                payload.PurchaseOrderId == order.Id &&
                payload.PreviousStatus == nameof(EPurchaseOrderStatus.Draft) &&
                payload.Status == nameof(EPurchaseOrderStatus.PendingManagerApproval) &&
                payload.TotalAmount == 1234.56m &&
                payload.Currency == "SGD"),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(EApprovalStage.Manager, EPurchaseOrderStatus.PendingFinanceApproval)]
    [InlineData(EApprovalStage.Finance, EPurchaseOrderStatus.PendingProcurementApproval)]
    public async Task Approving_a_stage_advances_to_the_next_one(
        EApprovalStage decidedStage,
        EPurchaseOrderStatus expectedStatus)
    {
        var order = SubmittedOrder();
        DecideEverythingBefore(order, decidedStage);
        Stub(order);

        await CreateController().ProcessApproval(
            new ApprovalActionDto
            {
                PurchaseOrderId = order.Id,
                Action = EApprovalAction.Approve,
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, order.Status);
    }

    [Fact]
    public async Task Approving_the_last_stage_approves_the_order()
    {
        var order = SubmittedOrder();
        DecideEverythingBefore(order, EApprovalStage.Procurement);
        Stub(order);

        await CreateController().ProcessApproval(
            new ApprovalActionDto
            {
                PurchaseOrderId = order.Id,
                Action = EApprovalAction.Approve,
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(EPurchaseOrderStatus.Approved, order.Status);
        Assert.All(order.Approvals, approval => Assert.NotNull(approval.Action));
    }

    [Fact]
    public async Task A_rejection_stops_the_chain_and_records_the_reason()
    {
        var order = SubmittedOrder();
        Stub(order);

        await CreateController().ProcessApproval(
            new ApprovalActionDto
            {
                PurchaseOrderId = order.Id,
                Action = EApprovalAction.Reject,
                Comments = "Budget exhausted",
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(EPurchaseOrderStatus.Rejected, order.Status);
        Assert.Equal("Budget exhausted", order.RejectionReason);
        Assert.Equal(
            2,
            order.Approvals.Count(approval => approval.Action is null));
    }

    [Fact]
    public async Task An_approval_records_the_acting_user_and_the_decision_time()
    {
        var order = SubmittedOrder();
        Stub(order);

        await CreateController().ProcessApproval(
            new ApprovalActionDto
            {
                PurchaseOrderId = order.Id,
                Action = EApprovalAction.Approve,
                Comments = "Looks good",
            },
            TestContext.Current.CancellationToken);

        var decided = order.Approvals.Single(approval => approval.ApprovalStage == EApprovalStage.Manager);
        Assert.Equal("user-1", decided.ApproverId);
        Assert.Equal("Ada Lovelace", decided.ApproverName);
        Assert.Equal("Looks good", decided.Comments);
        Assert.NotNull(decided.ActionDate);
    }

    [Fact]
    public async Task An_order_with_no_pending_stage_cannot_be_approved_again()
    {
        var order = SubmittedOrder();
        foreach (var approval in order.Approvals)
        {
            approval.Action = EApprovalAction.Approve;
        }

        order.Status = EPurchaseOrderStatus.Approved;
        Stub(order);

        var result = await CreateController().ProcessApproval(
            new ApprovalActionDto
            {
                PurchaseOrderId = order.Id,
                Action = EApprovalAction.Approve,
            },
            TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(EPurchaseOrderStatus.Approved, order.Status);
        await _publisher.DidNotReceive().EnqueueAsync(
            Arg.Any<IntegrationContractDescriptor>(),
            Arg.Any<PurchaseOrderStatusChangedV1>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(EPurchaseOrderStatus.PendingManagerApproval)]
    [InlineData(EPurchaseOrderStatus.Approved)]
    public async Task Only_a_draft_can_be_deleted(EPurchaseOrderStatus status)
    {
        var order = Draft();
        order.Status = status;
        Stub(order);

        var result = await CreateController().Delete(order.Id, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
        await _orders.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Deleting_a_draft_removes_its_stored_documents_first()
    {
        var order = Draft();
        order.Documents.Add(new PurchaseOrderDocument { FilePath = "orders/a.pdf" });
        order.Documents.Add(new PurchaseOrderDocument { FilePath = "orders/b.pdf" });
        Stub(order);
        _orders.DeleteAsync(order.Id).Returns(true);

        var result = await CreateController().Delete(order.Id, TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
        await _storage.Received(1).DeleteFileAsync("orders/a.pdf", Arg.Any<CancellationToken>());
        await _storage.Received(1).DeleteFileAsync("orders/b.pdf", Arg.Any<CancellationToken>());
        await _orders.Received(1).DeleteAsync(order.Id);
    }

    [Fact]
    public async Task A_storage_failure_does_not_block_deleting_the_order()
    {
        var order = Draft();
        order.Documents.Add(new PurchaseOrderDocument { FilePath = "orders/missing.pdf" });
        Stub(order);
        _storage
            .DeleteFileAsync("orders/missing.pdf", Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("object store unavailable"));
        _orders.DeleteAsync(order.Id).Returns(true);

        var result = await CreateController().Delete(order.Id, TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
        await _orders.Received(1).DeleteAsync(order.Id);
    }

    [Fact]
    public async Task A_failed_delete_is_reported_as_a_bad_request()
    {
        var order = Draft();
        Stub(order);
        _orders.DeleteAsync(order.Id).Returns(false);

        var result = await CreateController().Delete(order.Id, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Editing_rejects_an_empty_identifier_before_loading_anything()
    {
        var result = await CreateController().Edit(
            new PurchaseOrderDto { Id = Guid.Empty },
            TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await _orders.DidNotReceive().GetByIdWithDetailsAsync(
            Arg.Any<Guid>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(EPurchaseOrderStatus.PendingFinanceApproval)]
    [InlineData(EPurchaseOrderStatus.Approved)]
    public async Task Only_a_draft_can_be_edited(EPurchaseOrderStatus status)
    {
        var order = Draft();
        order.Status = status;
        Stub(order);

        var result = await CreateController().Edit(
            new PurchaseOrderDto { Id = order.Id },
            TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await _orders.DidNotReceive().SaveOrUpdateAsync(Arg.Any<PurchaseOrder>());
    }

    [Fact]
    public async Task Editing_replaces_the_lines_and_recalculates_the_order_total()
    {
        var order = Draft();
        order.Lines.Add(new PurchaseOrderLine { LineNumber = 1, ItemName = "old", Quantity = 1, UnitPrice = 5m, LineTotal = 5m });
        Stub(order);

        await CreateController().Edit(
            new PurchaseOrderDto
            {
                Id = order.Id,
                Lines =
                [
                    new PurchaseOrderLineDto { LineNumber = 1, ItemName = "paper", Quantity = 3, UnitPrice = 2.50m },
                    new PurchaseOrderLineDto { LineNumber = 2, ItemName = "toner", Quantity = 2, UnitPrice = 10m },
                ],
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, order.Lines.Count);
        Assert.DoesNotContain(order.Lines, line => line.ItemName == "old");
        Assert.Equal(27.50m, order.TotalAmount);
        Assert.Equal(7.50m, order.Lines.Single(line => line.ItemName == "paper").LineTotal);
    }

    [Fact]
    public async Task Editing_an_unknown_order_is_a_404()
    {
        _orders
            .GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((PurchaseOrder?)null);

        var result = await CreateController().Edit(
            new PurchaseOrderDto { Id = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private static PurchaseOrder Draft() =>
        new()
        {
            Id = Guid.CreateVersion7(),
            PoNumber = "PO-2026-00001",
            RequestedBy = "user-1",
            Status = EPurchaseOrderStatus.Draft,
        };

    private static PurchaseOrder SubmittedOrder()
    {
        var order = Draft();
        order.Status = EPurchaseOrderStatus.PendingManagerApproval;
        foreach (var stage in new[] { EApprovalStage.Manager, EApprovalStage.Finance, EApprovalStage.Procurement })
        {
            order.Approvals.Add(new PurchaseOrderApproval
            {
                ApprovalStage = stage,
                StageOrder = (int)stage,
                PurchaseOrderId = order.Id,
            });
        }

        return order;
    }

    private static void DecideEverythingBefore(PurchaseOrder order, EApprovalStage stage)
    {
        foreach (var approval in order.Approvals.Where(approval => approval.StageOrder < (int)stage))
        {
            approval.Action = EApprovalAction.Approve;
        }
    }

    private void Stub(PurchaseOrder order)
    {
        _orders
            .GetByIdWithDetailsAsync(order.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(order);
        _orders.SaveOrUpdateAsync(Arg.Any<PurchaseOrder>()).Returns(order);
        _orders.DeleteAsync(order.Id).Returns(true);
    }

    private PurchaseOrderController CreateController()
    {
        var httpContext = MvcTestContext.CreateHttpContext(userId: "user-1");
        httpContext.Items[BuildingBlocks.Globals.Constants.KeySessionUserName] = "Ada Lovelace";
        httpContext.TraceIdentifier = "trace-1";

        return new PurchaseOrderController(
            _orders,
            _documents,
            _storage,
            _mapper,
            _publisher,
            NullLogger<PurchaseOrderController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }
}
