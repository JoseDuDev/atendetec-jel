using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atendefy.API.Modules.Billing.Models;

public record PlanLimits(
    [property: JsonPropertyName("messages_per_month")] int MessagesPerMonth = 1000,
    [property: JsonPropertyName("whatsapp_accounts")] int WhatsAppAccounts = 1,
    [property: JsonPropertyName("team_members")] int TeamMembers = 3
)
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static PlanLimits FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new PlanLimits();
        try { return JsonSerializer.Deserialize<PlanLimits>(json, Opts) ?? new PlanLimits(); }
        catch { return new PlanLimits(); }
    }
}
