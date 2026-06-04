using Atendefy.API.Modules.Tenants.Models;
using Microsoft.EntityFrameworkCore;

namespace Atendefy.API.Infrastructure.Database;

public class PublicDbContext(DbContextOptions<PublicDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(x => x.Id);
            e.Property(x => x.Subdomain).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Subdomain).IsUnique();
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Ignore(x => x.SchemaName);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<TenantUser>(e =>
        {
            e.ToTable("tenant_users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            e.Property(x => x.Role).HasMaxLength(50);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
