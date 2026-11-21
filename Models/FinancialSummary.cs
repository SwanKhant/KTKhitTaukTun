namespace KhittoktunPortal.Models;

public sealed class FinancialSummary
{
    public int Id { get; set; }
    public decimal TotalMoney { get; set; }
    public decimal CurrentFyRevenue { get; set; }
    public decimal LastFyRevenue { get; set; }
    public decimal DueAmount { get; set; }
    public bool IsDuePaid { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? UpdatedByUserId { get; set; }
}
