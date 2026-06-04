using Microsoft.AspNetCore.Http;

namespace Atendefy.API.Infrastructure.Database;

public class TenantResolver(string baseDomain)
{
    private static readonly HashSet<string> PlatformSubdomains =
        new(StringComparer.OrdinalIgnoreCase) { "api", "app", "www", "evolution", "monitor" };

    public string? Resolve(HttpContext context)
    {
        var host = context.Request.Host.Host;

        if (host.EndsWith($".{baseDomain}", StringComparison.OrdinalIgnoreCase))
        {
            var subdomain = host[..^(baseDomain.Length + 1)];
            if (!PlatformSubdomains.Contains(subdomain))
                return subdomain;
        }

        if (context.Request.Headers.TryGetValue("X-Tenant-Key", out var key)
            && !string.IsNullOrWhiteSpace(key))
            return key.ToString();

        return null;
    }
}
