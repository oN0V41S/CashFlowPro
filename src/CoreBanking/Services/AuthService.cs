using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoreBanking.Data;
using CoreBanking.Domain.Accounts;
using CoreBanking.Domain.User;
using CoreBanking.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CoreBanking.Services;

public class AuthService(AppDbContext _db, IPasswordHasher<User> _passwordHasher, IConfiguration _configuration)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _db.Users
            .AnyAsync(u => u.Email == request.Email.ToLower().Trim());

        if (existingUser)
            throw new InvalidOperationException("Email já cadastrado no sistema.");

        var tempUser = (User)Activator.CreateInstance(typeof(User), true)!;
        var passwordHash = _passwordHasher.HashPassword(tempUser, request.Password);

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var account = Account.Open(request.Email.Split('@')[0], AccountType.Checking);
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var user = User.Register(account.Id, request.Email, passwordHash);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            return GenerateJwtToken(user);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim());

        if (user == null)
            throw new UnauthorizedAccessException("Email ou senha inválidos.");

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Email ou senha inválidos.");

        return GenerateJwtToken(user);
    }

    private AuthResponse GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key não configurada!");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expireHours = double.Parse(jwtSettings["ExpireHours"] ?? "8");
        var expiresAt = DateTime.UtcNow.AddHours(expireHours);

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("accountId", user.AccountId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse(tokenString, user.Email, user.Role, expiresAt);
    }
}
