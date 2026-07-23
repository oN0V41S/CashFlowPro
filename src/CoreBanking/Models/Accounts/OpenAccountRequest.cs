using CoreBanking.Domain.Accounts;

namespace CoreBanking.Models.Accounts;

public record OpenAccountRequest(
    string HolderName,
    AccountType Type
);