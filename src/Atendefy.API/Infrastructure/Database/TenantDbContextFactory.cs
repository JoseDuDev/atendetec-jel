using Microsoft.EntityFrameworkCore;

namespace Atendefy.API.Infrastructure.Database;

public class TenantDbContextFactory(string connectionString)
{
    public TenantDbContext Create(string schemaName)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new TenantDbContext(options, schemaName);
    }
}
