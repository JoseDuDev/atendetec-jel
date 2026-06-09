using Atendefy.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Atendefy.Tests.Integration;

public class InMemoryTenantDbContextFactory : TenantDbContextFactory
{
    public InMemoryTenantDbContextFactory() : base("inmemory") { }

    public override TenantDbContext Create(string schemaName) =>
        new(new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(schemaName)
            .Options, schemaName);
}
