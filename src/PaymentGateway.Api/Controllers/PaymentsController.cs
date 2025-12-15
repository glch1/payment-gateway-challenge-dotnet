using System.Net;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentsRepository _paymentsRepository;

    public PaymentsController(IPaymentService paymentService, IPaymentsRepository paymentsRepository)
    {
        _paymentService = paymentService;
        _paymentsRepository = paymentsRepository;
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> ProcessPaymentAsync([FromBody] PostPaymentRequest request)
    {
        try
        {
            var response = await _paymentService.ProcessPaymentAsync(request);

            // Payment processing results (Authorized, Declined) return 200 OK
            // The status is indicated in the response body
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            // Validation failed - return 400 Bad Request with error details
            return BadRequest(new { errors = ex.Errors });
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Bank service is unavailable") || ex.Message.Contains("Service Unavailable"))
        {
            // Bank returned 503 Service Unavailable
            return StatusCode(503, new { error = "Bank service is unavailable" });
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Bank request timed out") || ex.Message.Contains("timed out"))
        {
            // Bank request timed out
            return StatusCode(500, new { error = "Bank request timed out" });
        }
        catch (HttpRequestException)
        {
            // Other bank errors (400, invalid response, etc.) return 500
            return StatusCode(500, new { error = "An error occurred while processing the payment" });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = await _paymentsRepository.GetAsync(id);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }
}