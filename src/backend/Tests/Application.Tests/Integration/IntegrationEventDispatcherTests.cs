using System.Text.Json;
using Application.Integration;
using Contracts.Integration;
using NSubstitute;

namespace Application.Tests;

public sealed class IntegrationEventDispatcherTests
{
    private const string VendorEventName = "nie.vendor-master.vendor-profile.changed";
    private const string Consumer = "nie-template";

    private static IntegrationEventEnvelope CreateEnvelope(
        string eventName = VendorEventName,
        int eventVersion = 1) =>
        new()
        {
            MessageId = Guid.CreateVersion7(),
            EventName = eventName,
            EventVersion = eventVersion,
            Producer = "vendor-master",
            OccurredAtUtc = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero),
            CorrelationId = "correlation-1",
            CausationId = "causation-1",
            Data = JsonSerializer.SerializeToElement(new { vendorCode = "V-001" }),
        };

    private static IIntegrationEventHandler CreateHandler(
        string eventName = VendorEventName,
        int eventVersion = 1)
    {
        var handler = Substitute.For<IIntegrationEventHandler>();
        handler.Contract.Returns(new IntegrationContractDescriptor(
            eventName,
            eventVersion,
            typeof(object)));
        return handler;
    }

    [Fact]
    public async Task Routes_the_event_to_the_handler_that_owns_the_contract()
    {
        var handler = CreateHandler();
        var dispatcher = new IntegrationEventDispatcher([handler]);
        var envelope = CreateEnvelope();

        await dispatcher.DispatchAsync(envelope, Consumer, TestContext.Current.CancellationToken);

        await handler.Received(1).HandleAsync(
            Arg.Any<JsonElement>(),
            Arg.Any<IntegrationEventContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Copies_the_envelope_metadata_into_the_handler_context()
    {
        var handler = CreateHandler();
        var dispatcher = new IntegrationEventDispatcher([handler]);
        var envelope = CreateEnvelope();
        IntegrationEventContext? captured = null;
        handler
            .HandleAsync(
                Arg.Any<JsonElement>(),
                Arg.Do<IntegrationEventContext>(context => captured = context),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await dispatcher.DispatchAsync(envelope, Consumer, TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal(envelope.MessageId, captured.MessageId);
        Assert.Equal(envelope.EventName, captured.EventName);
        Assert.Equal(envelope.EventVersion, captured.EventVersion);
        Assert.Equal(envelope.Producer, captured.Producer);
        Assert.Equal(envelope.OccurredAtUtc, captured.OccurredAtUtc);
        Assert.Equal(envelope.CorrelationId, captured.CorrelationId);
        Assert.Equal(envelope.CausationId, captured.CausationId);
        Assert.Equal(Consumer, captured.Consumer);
    }

    [Fact]
    public async Task Rejects_an_event_with_no_registered_handler_as_permanent()
    {
        var dispatcher = new IntegrationEventDispatcher([]);

        var exception = await Assert.ThrowsAsync<PermanentIntegrationEventException>(async () =>
            await dispatcher.DispatchAsync(
                CreateEnvelope(),
                Consumer,
                TestContext.Current.CancellationToken));

        Assert.Contains(VendorEventName, exception.Message, StringComparison.Ordinal);
        Assert.Contains("v1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejects_an_event_whose_version_no_handler_supports()
    {
        var dispatcher = new IntegrationEventDispatcher([CreateHandler(eventVersion: 1)]);

        await Assert.ThrowsAsync<PermanentIntegrationEventException>(async () =>
            await dispatcher.DispatchAsync(
                CreateEnvelope(eventVersion: 2),
                Consumer,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Matches_the_event_name_case_sensitively()
    {
        var dispatcher = new IntegrationEventDispatcher([CreateHandler()]);

        await Assert.ThrowsAsync<PermanentIntegrationEventException>(async () =>
            await dispatcher.DispatchAsync(
                CreateEnvelope(eventName: "NIE.Vendor-Master.Vendor-Profile.Changed"),
                Consumer,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Refuses_to_choose_between_two_handlers_for_the_same_contract()
    {
        var dispatcher = new IntegrationEventDispatcher([CreateHandler(), CreateHandler()]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dispatcher.DispatchAsync(
                CreateEnvelope(),
                Consumer,
                TestContext.Current.CancellationToken));

        Assert.Contains("Multiple handlers", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejects_a_null_envelope()
    {
        var dispatcher = new IntegrationEventDispatcher([CreateHandler()]);

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await dispatcher.DispatchAsync(null!, Consumer, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rejects_a_missing_consumer_name(string? consumer)
    {
        var dispatcher = new IntegrationEventDispatcher([CreateHandler()]);

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await dispatcher.DispatchAsync(
                CreateEnvelope(),
                consumer!,
                TestContext.Current.CancellationToken));

        Assert.Equal("consumer", exception.ParamName);
    }

    [Fact]
    public async Task Leaves_handlers_for_other_contracts_untouched()
    {
        var matching = CreateHandler();
        var other = CreateHandler(eventName: "nie.procurement.purchase-order.status-changed");
        var dispatcher = new IntegrationEventDispatcher([other, matching]);

        await dispatcher.DispatchAsync(CreateEnvelope(), Consumer, TestContext.Current.CancellationToken);

        await matching.Received(1).HandleAsync(
            Arg.Any<JsonElement>(),
            Arg.Any<IntegrationEventContext>(),
            Arg.Any<CancellationToken>());
        await other.DidNotReceive().HandleAsync(
            Arg.Any<JsonElement>(),
            Arg.Any<IntegrationEventContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Forwards_the_cancellation_token_to_the_handler()
    {
        var handler = CreateHandler();
        var dispatcher = new IntegrationEventDispatcher([handler]);
        using var source = new CancellationTokenSource();

        await dispatcher.DispatchAsync(CreateEnvelope(), Consumer, source.Token);

        await handler.Received(1).HandleAsync(
            Arg.Any<JsonElement>(),
            Arg.Any<IntegrationEventContext>(),
            source.Token);
    }

    [Fact]
    public async Task Forwards_the_raw_payload_element_to_the_handler()
    {
        var handler = CreateHandler();
        var dispatcher = new IntegrationEventDispatcher([handler]);
        var envelope = CreateEnvelope();
        JsonElement captured = default;
        handler
            .HandleAsync(
                Arg.Do<JsonElement>(element => captured = element),
                Arg.Any<IntegrationEventContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await dispatcher.DispatchAsync(envelope, Consumer, TestContext.Current.CancellationToken);

        Assert.Equal("V-001", captured.GetProperty("vendorCode").GetString());
    }
}
