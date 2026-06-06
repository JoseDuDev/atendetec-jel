using Atendefy.API.Modules.Billing.Gateways;

namespace Atendefy.API.Modules.Billing;

public static class BillingWebhookEndpoints
{
    public static IEndpointRouteBuilder MapBillingWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/billing/webhooks").WithTags("Billing");

        // POST /billing/webhooks/asaas — token validation via "asaas-access-token" header
        group.MapPost("/asaas", async (
            HttpContext ctx,
            BillingService billingService,
            IBillingGatewayFactory gatewayFactory) =>
        {
            ctx.Request.EnableBuffering();
            var body = await ReadBytesAsync(ctx.Request.Body);
            ctx.Request.Body.Position = 0;

            var token = ctx.Request.Headers["asaas-access-token"].ToString();
            var gateway = gatewayFactory.Create("asaas");
            if (!gateway.ValidateWebhook(body, token))
                return Results.Forbid();

            var json = System.Text.Encoding.UTF8.GetString(body);
            var evt = gateway.ParseWebhookEvent(json);
            if (evt is not null)
                await billingService.ProcessPaymentEventAsync(evt);

            return Results.Ok();
        });

        // POST /billing/webhooks/stripe — HMAC-SHA256 validation via "Stripe-Signature" header
        group.MapPost("/stripe", async (
            HttpContext ctx,
            BillingService billingService,
            IBillingGatewayFactory gatewayFactory) =>
        {
            ctx.Request.EnableBuffering();
            var body = await ReadBytesAsync(ctx.Request.Body);
            ctx.Request.Body.Position = 0;

            var signature = ctx.Request.Headers["Stripe-Signature"].ToString();
            var gateway = gatewayFactory.Create("stripe");
            if (!gateway.ValidateWebhook(body, signature))
                return Results.Forbid();

            var json = System.Text.Encoding.UTF8.GetString(body);
            var evt = gateway.ParseWebhookEvent(json);
            if (evt is not null)
                await billingService.ProcessPaymentEventAsync(evt);

            return Results.Ok();
        });

        return app;
    }

    private static async Task<byte[]> ReadBytesAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}
