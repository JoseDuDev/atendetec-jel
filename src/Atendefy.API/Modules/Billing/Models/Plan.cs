using Atendefy.API.SharedKernel;

namespace Atendefy.API.Modules.Billing.Models;

public class Plan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public string LimitsJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}
