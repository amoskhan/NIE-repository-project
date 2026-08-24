using Application.Integration.Validation;
using Contracts.Events.VendorMaster;

namespace Application.Tests;

public sealed class VendorProfileChangedV1ValidatorTests
{
    private static readonly VendorProfileChangedV1Validator Validator = new();

    private static readonly DateTimeOffset ChangedAt =
        new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

    private static VendorProfileChangedV1 Message(
        string vendorCode = "V-001",
        string name = "Acme Supplies",
        string? contactPerson = "Ada",
        string? email = "ops@acme.test",
        string? phone = "+65 6000 0000",
        string? address = "1 Nanyang Walk",
        string? category = "Stationery",
        bool isActive = true,
        DateTimeOffset? changedAtUtc = null) =>
        new(
            vendorCode,
            name,
            contactPerson,
            email,
            phone,
            address,
            category,
            isActive,
            changedAtUtc ?? ChangedAt);

    private static List<string> Failures(VendorProfileChangedV1 message) =>
        Validator.Validate(message).Errors.Select(failure => failure.PropertyName).ToList();

    [Fact]
    public void Accepts_a_complete_vendor_profile_event()
    {
        Assert.True(Validator.Validate(Message()).IsValid);
    }

    [Fact]
    public void Accepts_a_vendor_profile_event_that_omits_every_optional_field()
    {
        var result = Validator.Validate(Message(
            contactPerson: null,
            email: null,
            phone: null,
            address: null,
            category: null));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_missing_vendor_code(string vendorCode)
    {
        Assert.Contains(nameof(VendorProfileChangedV1.VendorCode), Failures(Message(vendorCode: vendorCode)));
    }

    [Theory]
    [InlineData("-V001")]
    [InlineData(".V001")]
    [InlineData("_V001")]
    [InlineData("V 001")]
    [InlineData("V/001")]
    [InlineData("V001;DROP")]
    public void Rejects_a_vendor_code_outside_the_allowed_character_set(string vendorCode)
    {
        Assert.Contains(nameof(VendorProfileChangedV1.VendorCode), Failures(Message(vendorCode: vendorCode)));
    }

    [Theory]
    [InlineData("V001")]
    [InlineData("v001")]
    [InlineData("V-001")]
    [InlineData("V.001")]
    [InlineData("V_001")]
    [InlineData("1")]
    public void Accepts_a_vendor_code_that_starts_alphanumeric(string vendorCode)
    {
        Assert.DoesNotContain(nameof(VendorProfileChangedV1.VendorCode), Failures(Message(vendorCode: vendorCode)));
    }

    [Theory]
    [InlineData(50, false)]
    [InlineData(51, true)]
    public void Bounds_the_vendor_code_at_fifty_characters(int length, bool expectFailure)
    {
        var failures = Failures(Message(vendorCode: new string('V', length)));

        Assert.Equal(expectFailure, failures.Contains(nameof(VendorProfileChangedV1.VendorCode)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void Rejects_a_missing_vendor_name(string name)
    {
        Assert.Contains(nameof(VendorProfileChangedV1.Name), Failures(Message(name: name)));
    }

    [Theory]
    [InlineData(200, false)]
    [InlineData(201, true)]
    public void Bounds_the_vendor_name_at_two_hundred_characters(int length, bool expectFailure)
    {
        var failures = Failures(Message(name: new string('n', length)));

        Assert.Equal(expectFailure, failures.Contains(nameof(VendorProfileChangedV1.Name)));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.test")]
    public void Rejects_a_malformed_email_address(string email)
    {
        Assert.Contains(nameof(VendorProfileChangedV1.Email), Failures(Message(email: email)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Skips_the_email_format_rule_when_no_address_is_supplied(string? email)
    {
        Assert.DoesNotContain(nameof(VendorProfileChangedV1.Email), Failures(Message(email: email)));
    }

    [Theory]
    [InlineData(51, nameof(VendorProfileChangedV1.Phone))]
    [InlineData(50, null)]
    public void Bounds_the_phone_number_at_fifty_characters(int length, string? expectedFailure)
    {
        var failures = Failures(Message(phone: new string('9', length)));

        if (expectedFailure is null)
        {
            Assert.DoesNotContain(nameof(VendorProfileChangedV1.Phone), failures);
        }
        else
        {
            Assert.Contains(expectedFailure, failures);
        }
    }

    [Fact]
    public void Bounds_the_address_at_one_thousand_characters()
    {
        Assert.Contains(
            nameof(VendorProfileChangedV1.Address),
            Failures(Message(address: new string('a', 1001))));
        Assert.DoesNotContain(
            nameof(VendorProfileChangedV1.Address),
            Failures(Message(address: new string('a', 1000))));
    }

    [Fact]
    public void Bounds_the_category_at_one_hundred_characters()
    {
        Assert.Contains(
            nameof(VendorProfileChangedV1.Category),
            Failures(Message(category: new string('c', 101))));
    }

    [Fact]
    public void Bounds_the_contact_person_at_two_hundred_characters()
    {
        Assert.Contains(
            nameof(VendorProfileChangedV1.ContactPerson),
            Failures(Message(contactPerson: new string('p', 201))));
    }

    [Fact]
    public void Rejects_an_unset_change_timestamp()
    {
        Assert.Contains(
            nameof(VendorProfileChangedV1.ChangedAtUtc),
            Failures(Message(changedAtUtc: default(DateTimeOffset))));
    }

    [Fact]
    public void Stops_at_the_first_failed_rule_for_a_single_property()
    {
        var vendorCodeFailures = Failures(Message(vendorCode: string.Empty))
            .FindAll(property => property == nameof(VendorProfileChangedV1.VendorCode));

        Assert.Single(vendorCodeFailures);
    }

    [Fact]
    public void Reports_failures_for_several_properties_at_once()
    {
        var failures = Failures(Message(
            vendorCode: string.Empty,
            name: string.Empty,
            email: "nope",
            changedAtUtc: default(DateTimeOffset)));

        Assert.Contains(nameof(VendorProfileChangedV1.VendorCode), failures);
        Assert.Contains(nameof(VendorProfileChangedV1.Name), failures);
        Assert.Contains(nameof(VendorProfileChangedV1.Email), failures);
        Assert.Contains(nameof(VendorProfileChangedV1.ChangedAtUtc), failures);
    }
}
