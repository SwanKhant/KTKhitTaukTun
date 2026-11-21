namespace KhittoktunPortal.Models;

public sealed class ShareholderFinancialRecord
{
    public int Id { get; set; }
    public string ShareholderId { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public decimal TotalMoney { get; set; }
    public decimal Revenue { get; set; }
    public decimal DueAmount { get; set; }
    public bool IsDuePaid { get; set; }
    public DateTime ImportedAtUtc { get; set; }
    public string? ImportedByUserId { get; set; }
}
