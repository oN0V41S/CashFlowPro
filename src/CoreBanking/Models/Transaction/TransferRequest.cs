namespace CoreBanking.Models.Transaction;

public class TransferRequest
{
    public string ToAccountToken { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}