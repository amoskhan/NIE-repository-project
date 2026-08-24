using Auth.Models;
using Auth.Validation;
using FluentValidation.Results;

namespace Auth.Tests.Validation;

/// <summary>
/// Input validation is the first gate an anonymous caller meets, so the bounds that keep oversized
/// or empty credentials and payloads out of the identity provider are pinned here.
/// </summary>
public sealed class AuthRequestValidatorTests
{
    [Fact]
    public void A_well_formed_login_request_is_accepted()
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest { userid = "devia", pd = "secret" });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_login_request_without_a_user_identifier_is_rejected(string userid)
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest { userid = userid, pd = "secret" });

        AssertFailedOn(result, "userid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_login_request_without_a_password_is_rejected(string password)
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest { userid = "devia", pd = password });

        AssertFailedOn(result, "pd");
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void A_login_user_identifier_is_bounded(int length, bool expectedValid)
    {
        var result = new LoginRequestValidator().Validate(
            new LoginRequest { userid = new string('a', length), pd = "secret" });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(512, true)]
    [InlineData(513, false)]
    public void A_login_password_is_bounded(int length, bool expectedValid)
    {
        var result = new LoginRequestValidator().Validate(
            new LoginRequest { userid = "devia", pd = new string('a', length) });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void A_login_validation_failure_never_echoes_the_password()
    {
        var password = new string('p', 600);

        var result = new LoginRequestValidator().Validate(new LoginRequest { userid = "devia", pd = password });

        Assert.False(result.IsValid);
        foreach (var failure in result.Errors)
            Assert.DoesNotContain(password, failure.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void A_well_formed_sso_callback_is_accepted()
    {
        var result = new SsoCallbackRequestValidator().Validate(
            new SsoCallbackRequest { state = "state", encryptedPayload = "a.b.c.d.e" });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_sso_callback_without_a_state_is_rejected(string state)
    {
        var result = new SsoCallbackRequestValidator().Validate(
            new SsoCallbackRequest { state = state, encryptedPayload = "a.b.c.d.e" });

        AssertFailedOn(result, "state");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_sso_callback_without_a_payload_is_rejected(string payload)
    {
        var result = new SsoCallbackRequestValidator().Validate(
            new SsoCallbackRequest { state = "state", encryptedPayload = payload });

        AssertFailedOn(result, "encryptedPayload");
    }

    [Theory]
    [InlineData(512, true)]
    [InlineData(513, false)]
    public void An_sso_callback_state_is_bounded(int length, bool expectedValid)
    {
        var result = new SsoCallbackRequestValidator().Validate(
            new SsoCallbackRequest { state = new string('s', length), encryptedPayload = "a.b.c.d.e" });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(32_768, true)]
    [InlineData(32_769, false)]
    public void An_sso_callback_payload_is_bounded(int length, bool expectedValid)
    {
        var result = new SsoCallbackRequestValidator().Validate(
            new SsoCallbackRequest { state = "state", encryptedPayload = new string('p', length) });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void An_entirely_empty_test_session_request_is_accepted()
    {
        var result = new CreateTestSessionRequestValidator().Validate(new CreateTestSessionRequest());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("tester@nie.edu.sg", true)]
    [InlineData("not-an-email", false)]
    [InlineData("missing@", false)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData(null, true)]
    public void A_test_session_email_is_only_validated_when_it_is_supplied(string? email, bool expectedValid)
    {
        var result = new CreateTestSessionRequestValidator().Validate(new CreateTestSessionRequest { Email = email });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void An_oversized_test_session_email_is_rejected()
    {
        var email = new string('a', 310) + "@nie.edu.sg";

        var result = new CreateTestSessionRequestValidator().Validate(new CreateTestSessionRequest { Email = email });

        AssertFailedOn(result, "Email");
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void A_test_session_user_identifier_is_bounded(int length, bool expectedValid)
    {
        var result = new CreateTestSessionRequestValidator().Validate(
            new CreateTestSessionRequest { UserId = new string('u', length) });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void A_test_session_display_name_is_bounded(int length, bool expectedValid)
    {
        var result = new CreateTestSessionRequestValidator().Validate(
            new CreateTestSessionRequest { Name = new string('n', length) });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void A_test_session_department_is_bounded(int length, bool expectedValid)
    {
        var result = new CreateTestSessionRequestValidator().Validate(
            new CreateTestSessionRequest { Department = new string('d', length) });

        Assert.Equal(expectedValid, result.IsValid);
    }

    private static void AssertFailedOn(ValidationResult result, string propertyName)
    {
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => string.Equals(failure.PropertyName, propertyName, StringComparison.Ordinal));
    }
}
