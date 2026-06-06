using Atendefy.API.Modules.Billing.Gateways;
using Atendefy.Tests.Helpers;
using FluentAssertions;
using System.Security.Cryptography;
using System.Text;

namespace Atendefy.Tests.Billing;

public class StripeGatewayTests
{
    [Fact]
    public async Task CreateCustomerAsync_ShouldPostFormEncodedAndReturnId()
    {
        var handler = MockHttpMessageHandler.ReturnsJson("""{"id":"cus_stripe123","email":"test@test.com"}""");
        var gateway = new StripeGateway(new HttpClient(handler), "sk_test_key", "whsec_test");

        var id = await gateway.CreateCustomerAsync("Test Corp", "test@test.com", "12345678000190");

        id.Should().Be("cus_stripe123");
        var req = handler.Requests[0];
        req.RequestUri!.ToString().Should().Contain("/v1/customers");
        req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
        var body = await req.Content.ReadAsStringAsync();
        (body.Contains("name=Test+Corp") || body.Contains("name=Test%20Corp")).Should().BeTrue(
            $"body should contain URL-encoded name, but was: {body}");
    }

    [Fact]
    public async Task CreateChargeAsync_ShouldCreatePaymentIntentAndReturnClientSecret()
    {
        var response = """{"id":"pi_abc","status":"requires_payment_method","client_secret":"pi_abc_secret_xyz"}""";
        var handler = MockHttpMessageHandler.ReturnsJson(response);
        var gateway = new StripeGateway(new HttpClient(handler), "sk_test_key", "whsec_test");

        var charge = await gateway.CreateChargeAsync(new CreateChargeArgs(
            "cus_stripe123", 99.90m, "CREDIT_CARD", "Plano Starter - Mensal",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), "pm_card_visa"));

        charge.ExternalId.Should().Be("pi_abc");
        charge.ClientSecret.Should().Be("pi_abc_secret_xyz");
        charge.BoletoUrl.Should().BeNull();
        var body = await handler.Requests[0].Content!.ReadAsStringAsync();
        body.Should().Contain("amount=9990");  // 99.90 * 100 = 9990 cents
        body.Should().Contain("currency=brl");
    }

    [Fact]
    public void ValidateWebhook_WithCorrectSignature_ShouldReturnTrue()
    {
        var secret = "whsec_testsecret";
        var gateway = new StripeGateway(new HttpClient(), "sk_test", secret);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var payload = """{"type":"payment_intent.succeeded"}""";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signedPayload = $"{timestamp}.{payload}";
        var hash = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(signedPayload))).ToLower();
        var header = $"t={timestamp},v1={hash}";

        gateway.ValidateWebhook(payloadBytes, header).Should().BeTrue();
    }

    [Fact]
    public void ValidateWebhook_WithWrongSignature_ShouldReturnFalse()
    {
        var gateway = new StripeGateway(new HttpClient(), "sk_test", "whsec_real");
        var body = Encoding.UTF8.GetBytes("payload");
        gateway.ValidateWebhook(body, "t=12345,v1=invalidsignatureinvalidsignatureinvalidsig000000000000000").Should().BeFalse();
    }

    [Fact]
    public void ParseWebhookEvent_PaymentIntentSucceeded_ShouldReturnPaid()
    {
        var gateway = new StripeGateway(new HttpClient(), "sk_test", "whsec");
        var json = """{"type":"payment_intent.succeeded","data":{"object":{"id":"pi_001","status":"succeeded"}}}""";

        var evt = gateway.ParseWebhookEvent(json);

        evt!.ExternalId.Should().Be("pi_001");
        evt.IsPaid.Should().BeTrue();
        evt.IsOverdue.Should().BeFalse();
        evt.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void ParseWebhookEvent_PaymentIntentFailed_ShouldReturnOverdue()
    {
        var gateway = new StripeGateway(new HttpClient(), "sk_test", "whsec");
        var json = """{"type":"payment_intent.payment_failed","data":{"object":{"id":"pi_002","status":"requires_payment_method"}}}""";

        var evt = gateway.ParseWebhookEvent(json);

        evt!.IsOverdue.Should().BeTrue();
        evt.IsPaid.Should().BeFalse();
    }

    [Fact]
    public void ParseWebhookEvent_MalformedJson_ShouldReturnNull()
    {
        var gateway = new StripeGateway(new HttpClient(), "sk_test", "whsec");
        gateway.ParseWebhookEvent("not-json").Should().BeNull();
        gateway.ParseWebhookEvent("""{"type":"payment_intent.succeeded"}""").Should().BeNull(); // missing data.object
    }
}
