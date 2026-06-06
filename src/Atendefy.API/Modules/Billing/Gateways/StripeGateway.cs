using Atendefy.API.Modules.Billing.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Atendefy.API.Modules.Billing.Gateways;

public class StripeGateway : IBillingGateway
{
    private readonly HttpClient _httpClient;
    private readonly string _webhookSigningSecret;
    private const string BaseUrl = "https://api.stripe.com/v1";

    public StripeGateway(HttpClient httpClient, string secretKey, string webhookSigningSecret)
    {
        _httpClient = httpClient;
        _webhookSigningSecret = webhookSigningSecret;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secretKey);
    }

    public async Task<string> CreateCustomerAsync(string name, string email, string cpfCnpj)
    {
        var form = new FormUrlEncodedContent([
            new("name", name),
            new("email", email),
            new("metadata[cpfCnpj]", cpfCnpj)
        ]);
        var response = await _httpClient.PostAsync($"{BaseUrl}/customers", form);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetString()!;
    }

    public async Task<BillingCharge> CreateChargeAsync(CreateChargeArgs args)
    {
        var amountCents = (long)(args.Amount * 100);
        var fields = new List<KeyValuePair<string, string>>
        {
            new("amount", amountCents.ToString()),
            new("currency", "brl"),
            new("customer", args.CustomerExternalId),
            new("description", args.Description)
        };
        if (!string.IsNullOrEmpty(args.PaymentMethodId))
            fields.Add(new("payment_method", args.PaymentMethodId));

        var form = new FormUrlEncodedContent(fields);
        var response = await _httpClient.PostAsync($"{BaseUrl}/payment_intents", form);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("id").GetString()!;
        var clientSecret = json.TryGetProperty("client_secret", out var cs) ? cs.GetString() : null;

        return new BillingCharge(id, BoletoUrl: null, BoletoBarcode: null, PixCopyPaste: null, ClientSecret: clientSecret);
    }

    public async Task CancelChargeAsync(string externalId)
    {
        var form = new FormUrlEncodedContent([]);
        var response = await _httpClient.PostAsync($"{BaseUrl}/payment_intents/{externalId}/cancel", form);
        response.EnsureSuccessStatusCode();
    }

    public bool ValidateWebhook(byte[] payload, string headerValue)
    {
        // Header format: "t=timestamp,v1=signature"
        try
        {
            var parts = headerValue.Split(',');
            var tPart = parts.FirstOrDefault(p => p.StartsWith("t="))?[2..];
            var v1Part = parts.FirstOrDefault(p => p.StartsWith("v1="))?[3..];
            if (tPart is null || v1Part is null) return false;

            var signedPayload = $"{tPart}.{Encoding.UTF8.GetString(payload)}";
            var expectedHash = Convert.ToHexString(
                HMACSHA256.HashData(
                    Encoding.UTF8.GetBytes(_webhookSigningSecret),
                    Encoding.UTF8.GetBytes(signedPayload))).ToLower();

            var v1Lower = v1Part.ToLower();
            if (v1Lower.Length != expectedHash.Length) return false;

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(v1Lower),
                Encoding.UTF8.GetBytes(expectedHash));
        }
        catch
        {
            return false;
        }
    }

    public WebhookEvent? ParseWebhookEvent(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var type = doc.RootElement.GetProperty("type").GetString() ?? "";
            var obj = doc.RootElement.GetProperty("data").GetProperty("object");
            var id = obj.GetProperty("id").GetString()!;

            return new WebhookEvent(
                ExternalId: id,
                IsPaid: type == "payment_intent.succeeded",
                IsOverdue: type == "payment_intent.payment_failed",
                IsCancelled: type == "payment_intent.canceled"
            );
        }
        catch
        {
            return null;
        }
    }
}
