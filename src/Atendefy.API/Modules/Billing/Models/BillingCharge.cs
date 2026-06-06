namespace Atendefy.API.Modules.Billing.Models;

public record BillingCharge(
    string ExternalId,
    string? BoletoUrl,
    string? BoletoBarcode,
    string? PixCopyPaste,
    string? ClientSecret    // Stripe Payment Intent client_secret
);
