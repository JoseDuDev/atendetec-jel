namespace Atendefy.API.Modules.AI.Models;

public class AiConfig
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ApiKeyEncrypted { get; set; }
    public string? Model { get; set; }
    public string? SystemPrompt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
