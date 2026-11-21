namespace KhittoktunPortal.Models;

public sealed class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
