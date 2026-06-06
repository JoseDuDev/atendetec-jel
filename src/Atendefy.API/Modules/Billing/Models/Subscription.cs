using Atendefy.API.SharedKernel;

namespace Atendefy.API.Modules.Billing.Models;

public class Subscription : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; } = "pending";         // pending|active|past_due|suspended|cancelled
    public string BillingCycle { get; set; } = "monthly";   // monthly|yearly
    public string Provider { get; set; } = string.Empty;    // asaas|stripe
    public string? ExternalCustomerId { get; set; }         // customer ID no provider
    public string? ExternalId { get; set; }                 // last charge/payment ID no provider
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
}
