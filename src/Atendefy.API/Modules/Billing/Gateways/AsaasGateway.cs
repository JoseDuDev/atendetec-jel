using Atendefy.API.Modules.Billing.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Atendefy.API.Modules.Billing.Gateways;

public class AsaasGateway : IBillingGateway
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _webhookToken;

    public AsaasGateway(HttpClient httpClient, string apiKey, string webhookToken, bool isSandbox = false)
    {
        _httpClient = httpClient;
        _webhookToken = webhookToken;
        _baseUrl = isSandbox
            ? "https://sandbox.asaas.com/api/v3"
            : "https://api.asaas.com/v3";
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("access_token", apiKey);
    }

    public async Task<string> CreateCustomerAsync(string name, string email, string cpfCnpj)
    {
        var payload = new { name, email, cpfCnpj };
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/customers", payload);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetString()!;
    }

    public async Task<BillingCharge> CreateChargeAsync(CreateChargeArgs args)
    {
        var payload = new
        {
            customer = args.CustomerExternalId,
            billingType = args.BillingType,
            value = args.Amount,
            dueDate = args.DueDate.ToString("yyyy-MM-dd"),
            description = args.Description
        };
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/payments", payload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("id").GetString()!;

        string? boletoUrl = null, boletoBarcode = null, pixCopyPaste = null;

        if (json.TryGetProperty("bankSlipUrl", out var bsUrl))
            boletoUrl = bsUrl.GetString();

        if (json.TryGetProperty("identificationField", out var idField))
            boletoBarcode = idField.GetString();

        if (json.TryGetProperty("pixTransaction", out var pixTx) &&
            pixTx.TryGetProperty("qrCode", out var qr) &&
            qr.TryGetProperty("payload", out var pixPayload))
            pixCopyPaste = pixPayload.GetString();

        return new BillingCharge(id, boletoUrl, boletoBarcode, pixCopyPaste, ClientSecret: null);
    }

    public async Task CancelChargeAsync(string externalId)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/payments/{externalId}");
        response.EnsureSuccessStatusCode();
    }

    // Asaas uses static token comparison; payload is intentionally unused (no HMAC required)
    public bool ValidateWebhook(byte[] payload, string headerValue)
        => headerValue == _webhookToken;

    public WebhookEvent? ParseWebhookEvent(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var evt = doc.RootElement.GetProperty("event").GetString() ?? "";
            var payment = doc.RootElement.GetProperty("payment");
            var id = payment.GetProperty("id").GetString()!;

            return new WebhookEvent(
                ExternalId: id,
                IsPaid: evt == "PAYMENT_RECEIVED",
                IsOverdue: evt == "PAYMENT_OVERDUE",
                IsCancelled: evt == "PAYMENT_DELETED"
            );
        }
        catch
        {
            return null;
        }
    }
}
