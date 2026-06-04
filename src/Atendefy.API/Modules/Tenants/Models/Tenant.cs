using Atendefy.API.SharedKernel;

namespace Atendefy.API.Modules.Tenants.Models;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string Status { get; set; } = "active"; // active | suspended | cancelled
    public Guid? PlanId { get; set; }
    public string SchemaName => $"tenant_{Id:N}";
}
