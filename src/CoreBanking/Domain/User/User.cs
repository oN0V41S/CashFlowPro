namespace CoreBanking.Domain.User;

public class User
{
    public Guid Id { get; private set; }    
    public Guid AccountId { get; private set; } = Guid.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "User";
    public DateTime CreatedAt { get; private set; }

    private User() {}

    public static User Register(Guid accountId, string email, string passwordHash, string role = "User")
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("The email is required!", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("The passwordHash cannot be empty!", nameof(passwordHash));
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId is required!", nameof(accountId));

        return new User
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }
}
