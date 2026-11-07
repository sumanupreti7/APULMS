namespace APULMS.Models;

public class ContactInquiry
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "new";

    public string? AdminReply { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RepliedAt { get; set; }
}
