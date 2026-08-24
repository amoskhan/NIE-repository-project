using Api.Grpc;
using Api.Grpc.Validation;
using Application.Integration;
using Contracts.Grpc.Procurement.V1;
using Grpc.Core;

namespace Integration.Tests.Grpc;

public sealed class ProcurementQueryGrpcServiceTests
{
    [Fact]
    public async Task Invalid_identifier_is_rejected_before_query_execution()
    {
        var query = new FakeQuery(null);
        var service = new ProcurementQueryGrpcService(
            query,
            new GetPurchaseOrderSummaryRequestValidator());

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetPurchaseOrderSummary(
                new GetPurchaseOrderSummaryRequest { PurchaseOrderId = Guid.NewGuid().ToString("D") },
                new TestServerCallContext()));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Equal("The request is invalid.", exception.Status.Detail);
        Assert.Equal(0, query.CallCount);
    }

    [Fact]
    public async Task Summary_is_mapped_without_decimal_precision_loss()
    {
        var purchaseOrderId = Guid.CreateVersion7();
        var vendorId = Guid.CreateVersion7();
        var query = new FakeQuery(new ProcurementPurchaseOrderSummary(
            purchaseOrderId,
            "PO-2026-00421",
            "Approved",
            vendorId,
            "NIE Supplier",
            1234567890.1234m,
            "SGD"));
        var service = new ProcurementQueryGrpcService(
            query,
            new GetPurchaseOrderSummaryRequestValidator());

        var reply = await service.GetPurchaseOrderSummary(
            new GetPurchaseOrderSummaryRequest { PurchaseOrderId = purchaseOrderId.ToString("D") },
            new TestServerCallContext());

        Assert.Equal(purchaseOrderId.ToString("D"), reply.PurchaseOrderId);
        Assert.Equal(vendorId.ToString("D"), reply.VendorId);
        Assert.Equal("1234567890.1234", reply.TotalAmount);
        Assert.Equal(1, query.CallCount);
    }

    private sealed class FakeQuery(ProcurementPurchaseOrderSummary? result) : IProcurementIntegrationQuery
    {
        public int CallCount { get; private set; }

        public Task<ProcurementPurchaseOrderSummary?> GetPurchaseOrderSummaryAsync(
            Guid purchaseOrderId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly Metadata _requestHeaders = [];
        private readonly Metadata _responseTrailers = [];
        private Status _status;
        private WriteOptions? _writeOptions;

        protected override string MethodCore => "test";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "ipv4:127.0.0.1:1";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => _requestHeaders;
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => _responseTrailers;
        protected override Status StatusCore { get => _status; set => _status = value; }
        protected override WriteOptions? WriteOptionsCore { get => _writeOptions; set => _writeOptions = value; }
        protected override AuthContext AuthContextCore =>
            new("anonymous", new Dictionary<string, List<AuthProperty>>());

        protected override ContextPropagationToken CreatePropagationTokenCore(
            ContextPropagationOptions? options) =>
            throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
            Task.CompletedTask;
    }
}
