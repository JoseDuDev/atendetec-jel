namespace Atendefy.API.Modules.Chatbot.Models;

public class UsageCounter
{
    public string Month { get; set; } = string.Empty;
    public int MessagesSent { get; set; }
    public long TokensConsumed { get; set; }
    public decimal CostUsd { get; set; }
}
