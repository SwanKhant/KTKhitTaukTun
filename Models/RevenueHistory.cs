namespace KhittoktunPortal.Models;

public sealed class RevenueHistory
{
    public int Id { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
