using Atendefy.API.Infrastructure.Database;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Atendefy.Tests.Infrastructure;

public class TenantResolverTests
{
    [Fact]
    public void Resolve_FromSubdomain_ShouldReturnSubdomainAsId()
    {
        var context = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Host.Returns(new HostString("acme.atendefy.com.br"));
        request.Headers.Returns(new HeaderDictionary());
        context.Request.Returns(request);

        var resolver = new TenantResolver("atendefy.com.br");
        var tenantId = resolver.Resolve(context);

        tenantId.Should().Be("acme");
    }

    [Fact]
    public void Resolve_FromHeader_WhenNotTenantSubdomain_ShouldReturnHeaderValue()
    {
        var context = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Host.Returns(new HostString("api.atendefy.com.br"));
        request.Headers.Returns(new HeaderDictionary { { "X-Tenant-Key", "tenant_xyz" } });
        context.Request.Returns(request);

        var resolver = new TenantResolver("atendefy.com.br");
        var tenantId = resolver.Resolve(context);

        tenantId.Should().Be("tenant_xyz");
    }

    [Fact]
    public void Resolve_PlatformSubdomain_ShouldFallbackToHeader()
    {
        var context = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Host.Returns(new HostString("api.atendefy.com.br"));
        request.Headers.Returns(new HeaderDictionary());
        context.Request.Returns(request);

        var resolver = new TenantResolver("atendefy.com.br");
        var tenantId = resolver.Resolve(context);

        tenantId.Should().BeNull();
    }
}
