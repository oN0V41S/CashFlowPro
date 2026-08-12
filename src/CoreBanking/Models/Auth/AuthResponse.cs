namespace CoreBanking.Models.Auth;

public record AuthResponse(string Token, string Email, string Role, DateTime ExpiresAt);
