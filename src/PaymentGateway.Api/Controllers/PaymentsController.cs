using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentsRepository _paymentsRepository;

    public PaymentsController(IPaymentsRepository paymentsRepository)
    {
        _paymentsRepository = paymentsRepository;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = await _paymentsRepository.GetAsync(id);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }
}