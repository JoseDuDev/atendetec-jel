using Microsoft.EntityFrameworkCore;

namespace Atendefy.API.Infrastructure.Database;

public class TenantDbContext(DbContextOptions<TenantDbContext> options, string schema) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(schema);
        // Entidades do tenant são provisionadas via SQL direto no TenantProvisioner (Task 7)
    }
}
