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
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Atendefy/1.0");
    }

    public async Task<string> CreateCustomerAsync(string name, string email, string cpfCnpj)
    {
        var body = JsonSerializer.Serialize(new { name, email, cpfCnpj });
        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUrl}/customers", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Asaas {(int)response.StatusCode} [body={body}]: {responseBody}", null, response.StatusCode);
        return JsonDocument.Parse(responseBody).RootElement.GetProperty("id").GetString()!;
    }

    public async Task<BillingCharge> CreateChargeAsync(CreateChargeArgs args)
    {
        var body = JsonSerializer.Serialize(new
        {
            customer = args.CustomerExternalId,
            billingType = args.BillingType,
            value = args.Amount,
            dueDate = args.DueDate.ToString("yyyy-MM-dd"),
            description = args.Description
        });
        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUrl}/payments", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Asaas {(int)response.StatusCode} [body={body}]: {responseBody}", null, response.StatusCode);

        var json = JsonDocument.Parse(responseBody).RootElement;
        var id = json.GetProperty("id").GetString()!;

        string? boletoUrl = null, boletoBarcode = null, pixCopyPaste = null;

        if (json.TryGetProperty("bankSlipUrl", out var bsUrl))
            boletoUrl = bsUrl.GetString();

        if (json.TryGetProperty("identificationField", out var idField))
            boletoBarcode = idField.GetString();

        if (json.TryGetProperty("pixTransaction", out var pixTx) &&
            pixTx.ValueKind == JsonValueKind.Object &&
            pixTx.TryGetProperty("qrCode", out var qr) &&
            qr.ValueKind == JsonValueKind.Object &&
            qr.TryGetProperty("payload", out var pixPayload))
            pixCopyPaste = pixPayload.GetString();

        return new BillingCharge(id, boletoUrl, boletoBarcode, pixCopyPaste, ClientSecret: null);
    }

    public async Task CancelChargeAsync(string externalId)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/payments/{externalId}");
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Asaas {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }
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
