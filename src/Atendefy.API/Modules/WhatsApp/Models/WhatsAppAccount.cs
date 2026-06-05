namespace Atendefy.API.Modules.WhatsApp.Models;

public class WhatsAppAccount
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ConfigJson { get; set; }
    public string Status { get; set; } = "disconnected";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
