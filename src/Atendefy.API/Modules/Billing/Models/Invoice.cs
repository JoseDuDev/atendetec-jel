using Atendefy.API.SharedKernel;

namespace Atendefy.API.Modules.Billing.Models;

public class Invoice : BaseEntity
{
    public Guid SubscriptionId { get; set; }
    public Guid TenantId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending";         // pending|paid|overdue|cancelled
    public string Provider { get; set; } = string.Empty;    // asaas|stripe
    public string BillingType { get; set; } = string.Empty; // BOLETO|PIX|CREDIT_CARD
    public string? ExternalId { get; set; }                 // payment ID no provider
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? BoletoUrl { get; set; }
    public string? BoletoBarcode { get; set; }
    public string? PixCopyPaste { get; set; }
    public string? ClientSecret { get; set; }               // Stripe Payment Intent client_secret
}
