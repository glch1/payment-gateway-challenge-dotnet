using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Services;

using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Configure request size limits to prevent DoS attacks
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024; // 1KB max for form data
});
builder.Services.AddSwaggerGen(c =>
{
    // Include XML comments in Swagger documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add API title and description
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Payment Gateway API",
        Version = "v1",
        Description = "API for processing and retrieving payment transactions."
    });
});

builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Register HttpClient for BankClient with interface and added retry and circuit breaker
// pattern for resilience.
var httpClientBuilder = builder.Services.AddHttpClient<IBankClient, BankClient>()
    .AddPolicyHandler((serviceProvider, request) => GetRetryPolicy(serviceProvider.GetRequiredService<ILogger<Program>>()))
    .AddPolicyHandler(GetCircuitBreakerPolicy());

// Add health checks for monitoring and load balancer health probes
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Health check endpoint for load balancers and monitoring
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();

// Retries up to 3 times with exponential backof 2 -> 4 -> 8 seconds.
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable) 
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var statusCode = outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.GetType().Name ?? "Unknown";
                logger.LogWarning(
                    "Bank request failed. Retrying {RetryCount}/3 after {DelaySeconds}s. Status: {StatusCode}",
                    retryCount, timespan.TotalSeconds, statusCode);
            });
}

// Opens circuit after 5 consceutive failures, stays open for 30 seconds.
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (result, duration) =>
            {
                var statusCode = result.Result?.StatusCode.ToString() ?? result.Exception?.GetType().Name ?? "Unknown";
                System.Diagnostics.Debug.WriteLine(
                    $"[CIRCUIT BREAKER] Circuit breaker opened for {duration.TotalSeconds}s due to repeated failures. Last status: {statusCode}");
            },
            onReset: () =>
            {
                System.Diagnostics.Debug.WriteLine("[CIRCUIT BREAKER] Circuit breaker reset - bank service appears to be available again");
            },
            onHalfOpen: () =>
            {
                System.Diagnostics.Debug.WriteLine("[CIRCUIT BREAKER] Circuit breaker half-open - testing bank service connection");
            });
}
