using System.Text;
using Microsoft.AspNetCore.Authorization;
using CoreBanking.Services;
using CoreBanking.Models.Transaction;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace CoreBanking.Controllers;

[Authorize]
[ApiController]
[Route("api/transfers")]
public class TransfersController(
    TransferService _transferService,
    IConfiguration _configuration) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        try
        {
            var accountIdClaim = User.FindFirst("accountId")?.Value;

            if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var fromAccountId))
            {
                return BadRequest(new { error = "Conta não selecionada ou token inválido." });
            }

            var receiverAccountId = ExtractAccountIdFromToken(request.ToAccountToken);

            if (receiverAccountId == null)
            {
                return BadRequest(new { error = "O Token de Destino é inválido, expirado ou adulterado." });
            }

            var toAccountId = receiverAccountId.Value;

            if (fromAccountId == toAccountId)
            {
                return BadRequest(new { error = "Não é possível transferir para a mesma conta." });
            }

            await _transferService.TransferAsync(
                fromAccountId,
                toAccountId,
                request.Amount,
                request.Description
            );

            return Ok(new { message = "Transferência concluída com sucesso!" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid? ExtractAccountIdFromToken(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"];
            if (string.IsNullOrEmpty(secretKey)) return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            var accountIdStr = jwtToken.Claims
                .FirstOrDefault(x => x.Type == "accountId")?.Value;

            if (Guid.TryParse(accountIdStr, out var accountId))
            {
                return accountId;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
