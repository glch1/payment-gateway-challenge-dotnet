using System.Net;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Mappings;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

/// <summary>
/// Service for processing payment requests.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IBankClient _bankClient;
    private readonly IPaymentsRepository _repository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IBankClient bankClient, IPaymentsRepository repository, ILogger<PaymentService> logger)
    {
        _bankClient = bankClient;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Processes a payment request: validates, calls bank, stores result, and returns response.
    /// </summary>
    /// <param name="request">The payment request to process</param>
    /// <returns>The payment response with status and details</returns>
    public async Task<PostPaymentResponse> ProcessPaymentAsync(PostPaymentRequest request)
    {
        var paymentId = Guid.NewGuid();
        _logger.LogDebug("Processing payment request. PaymentId: {PaymentId}, Amount: {Amount}, Currency: {Currency}",
            paymentId, request.Amount, request.Currency);

        // Step 1: Validate the request
        var validationResult = PaymentValidator.Validate(request);
        if (!validationResult.IsValid)
        {
            _logger.LogInformation("Payment request validation failed. PaymentId: {PaymentId}, Errors: {Errors}",
                paymentId, string.Join("; ", validationResult.Errors));

            // Throw exception with validation errors - controller will return 400 Bad Request
            throw new ValidationException(validationResult.Errors);
        }

        _logger.LogDebug("Payment request validation passed. PaymentId: {PaymentId}", paymentId);

        // Step 2: Mask card number (extract last 4 digits)
        var maskedCardNumber = PaymentHelper.MaskCardNumber(request.CardNumber);
        _logger.LogDebug("Card number masked. PaymentId: {PaymentId}", paymentId);

        // Step 3: Map to bank request format
        var bankRequest = PaymentMappings.MapToBankRequest(request);

        // Step 4: Call bank
        BankPaymentResponse bankResponse;
        string status;
        try
        {
            bankResponse = await _bankClient.ProcessPaymentAsync(bankRequest);
            status = bankResponse.Authorized ? PaymentStatus.Authorized.ToString() : PaymentStatus.Declined.ToString();

            _logger.LogDebug("Bank response received. PaymentId: {PaymentId}, Authorized: {Authorized}, AuthorizationCode: {AuthorizationCode}",
                paymentId, bankResponse.Authorized, bankResponse.AuthorizationCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Bank error occurred. PaymentId: {PaymentId}", paymentId);
            // Bank errors (503, timeouts, etc.) - don't store, re-throw to be handled by controller
            throw;
        }

        // Step 5: Create response and store (only if Authorized or Declined)
        var paymentResponse = new PostPaymentResponse
        {
            Id = paymentId,
            Status = status,
            CardNumberLastFour = maskedCardNumber,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount
        };

        // Only store payments that made it to the bank (Authorized/Declined)
        await _repository.AddAsync(paymentResponse);
        _logger.LogDebug("Payment stored in repository. PaymentId: {PaymentId}, Status: {Status}",
            paymentId, status);

        _logger.LogInformation("Payment processed successfully. PaymentId: {PaymentId}, Status: {Status}, Amount: {Amount}, Currency: {Currency}",
            paymentId, status, request.Amount, request.Currency);

        return paymentResponse;
    }
}

