namespace WPFLLM.Models;

public class ChatMessage
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Embedding { get; set; }
}

public class MessageSearchResult
{
    public ChatMessage Message { get; set; } = null!;
    public Conversation Conversation { get; set; } = null!;
    public double Score { get; set; }
}

public class Conversation
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
