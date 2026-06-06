using Atendefy.API.Infrastructure.Database;
using Atendefy.API.Modules.Billing.Models;
using Atendefy.API.Modules.Tenants.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atendefy.API.Modules.Billing;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/billing").WithTags("Billing");

        // GET /billing/plans — public (no auth required)
        group.MapGet("/plans", async (PublicDbContext db) =>
        {
            var plans = await db.Plans
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.PriceMonthly,
                    p.PriceYearly,
                    p.LimitsJson
                })
                .ToListAsync();
            return Results.Ok(plans);
        });

        // POST /billing/subscribe — requires auth
        group.MapPost("/subscribe", async (
            [FromBody] CreateSubscriptionRequest request,
            BillingService billingService,
            PublicDbContext db,
            HttpContext ctx) =>
        {
            var (tenantId, tenant, error) = await ResolveTenantAsync(ctx, db);
            if (error is not null) return Results.Json(new { error }, statusCode: 401);

            var email = ctx.User.FindFirst("email")?.Value ?? string.Empty;
            var result = await billingService.SubscribeAsync(tenant!.Id, tenant.Name, email, request);

            return result.IsSuccess
                ? Results.Ok(new
                {
                    result.Value!.Id,
                    result.Value.Status,
                    result.Value.BoletoUrl,
                    result.Value.BoletoBarcode,
                    result.Value.PixCopyPaste,
                    result.Value.ClientSecret,
                    result.Value.DueDate
                })
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization();

        // GET /billing/subscription — requires auth
        group.MapGet("/subscription", async (
            PublicDbContext db,
            HttpContext ctx) =>
        {
            var (tenantId, _, error) = await ResolveTenantAsync(ctx, db);
            if (error is not null) return Results.Json(new { error }, statusCode: 401);

            var sub = await db.Subscriptions
                .Where(s => s.TenantId == tenantId && s.Status != "cancelled")
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (sub is null) return Results.NotFound(new { error = "Nenhuma assinatura ativa encontrada" });

            var plan = await db.Plans.FindAsync(sub.PlanId);
            var lastInvoice = await db.Invoices
                .Where(i => i.SubscriptionId == sub.Id)
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefaultAsync();

            return Results.Ok(new
            {
                sub.Id,
                sub.Status,
                sub.BillingCycle,
                sub.Provider,
                sub.CurrentPeriodStart,
                sub.CurrentPeriodEnd,
                Plan = plan is null ? null : new { plan.Id, plan.Name } as object,
                LastInvoice = lastInvoice is null ? null : new
                {
                    lastInvoice.Id,
                    lastInvoice.Status,
                    lastInvoice.Amount,
                    lastInvoice.DueDate,
                    lastInvoice.PaidAt
                } as object
            });
        }).RequireAuthorization();

        // DELETE /billing/subscription — requires auth
        group.MapDelete("/subscription", async (
            BillingService billingService,
            PublicDbContext db,
            HttpContext ctx) =>
        {
            var (tenantId, _, error) = await ResolveTenantAsync(ctx, db);
            if (error is not null) return Results.Json(new { error }, statusCode: 401);

            var result = await billingService.CancelAsync(tenantId);
            return result.IsSuccess
                ? Results.Ok(new { message = "Assinatura cancelada com sucesso" })
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization();

        return app;
    }

    private static async Task<(Guid TenantId, Tenant? Tenant, string? Error)>
        ResolveTenantAsync(HttpContext ctx, PublicDbContext db)
    {
        var tenantIdStr = ctx.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
            return (Guid.Empty, null, "Token inválido");

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        return tenant is null
            ? (Guid.Empty, null, "Tenant não encontrado")
            : (tenantId, tenant, null);
    }
}
