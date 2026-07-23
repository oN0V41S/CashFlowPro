using CoreBanking.Services;
using CoreBanking.Models.Transaction;
using Microsoft.AspNetCore.Mvc;

namespace CoreBanking.Controllers;

[ApiController]
[Route("api/transfers")]
public class TransfersController : ControllerBase
{
    private readonly TransferService _transferService;

    public TransfersController(TransferService transferService)
    {
        _transferService = transferService;
    }

    [HttpPost]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        try
        {
            await _transferService.TransferAsync(
                request.FromAccountId,
                request.ToAccountId,
                request.Amount,
                request.Description
            );

            return Ok(new { message = "Transferência Concluída com Sucesso!"});
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
}