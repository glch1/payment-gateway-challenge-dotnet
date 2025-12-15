using System.Net;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Controllers;

/// <summary>
/// Controller for processing and retrieving payments.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentsRepository _paymentsRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentsController"/> class.
    /// </summary>
    public PaymentsController(IPaymentService paymentService, IPaymentsRepository paymentsRepository)
    {
        _paymentService = paymentService;
        _paymentsRepository = paymentsRepository;
    }

    /// <summary>
    /// Processes a payment request.
    /// </summary>
    /// <param name="request">The payment request containing card details and amount.</param>
    /// <returns>
    /// <para>200 OK - Payment processed successfully (Authorized or Declined status in response body).</para>
    /// <para>400 Bad Request - Validation errors in the request.</para>
    /// <para>500 Internal Server Error - Bank request timed out or other bank errors.</para>
    /// <para>503 Service Unavailable - Bank service is unavailable.</para>
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
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

    /// <summary>
    /// Retrieves a payment by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the payment.</param>
    /// <returns>
    /// <para>200 OK - Payment found and returned.</para>
    /// <para>404 Not Found - Payment with the specified ID does not exist.</para>
    /// </returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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