# Payment Gateway API

A .NET 8 payment gateway API that processes payment requests and retrieves payment details. The API validates payment requests, communicates with a bank simulator, and stores payment information securely.

## Prerequisites

- .NET 8 SDK
- Docker and Docker Compose (for bank simulator)

## Getting Started

### 1. Start the Bank Simulator

The bank simulator must be running before starting the API (ensure you have Docker desktop running for this to work):

```bash
docker-compose up -d
```

This starts Mountebank on `http://localhost:8080`.

### 2. Run the API

```bash
cd src/PaymentGateway.Api
dotnet run
```

The API will be available at `https://localhost:5001` (or `http://localhost:5000`).

### 3. API Documentation

Swagger UI is available at:
- `https://localhost:5001/swagger` (HTTPS)
- `http://localhost:5000/swagger` (HTTP)

## Testing

Run all tests:

```bash
dotnet test
```

The test suite includes:
- **Unit tests**: Payment validation, service logic, helpers
- **Integration tests**: End-to-end API flow with mocked bank client

## API Endpoints

### POST /api/Payments

Process a payment request.

**Request Body:**
```json
{
  "cardNumber": "1234567890123457",
  "expiryMonth": 12,
  "expiryYear": 2025,
  "currency": "GBP",
  "amount": 1000,
  "cvv": "123"
}
```

**Response Codes:**
- `200 OK` - Payment processed (Authorized or Declined)
- `400 Bad Request` - Validation errors
- `500 Internal Server Error` - Bank errors
- `503 Service Unavailable` - Bank service unavailable

### GET /api/Payments/{id}

Retrieve payment details by ID.

**Response Codes:**
- `200 OK` - Payment found
- `404 Not Found` - Payment not found

## Features

- ✅ Payment validation (card number, expiry, currency, amount, CVV)
- ✅ Bank integration with retry and circuit breaker policies
- ✅ Secure card number masking (last 4 digits only)
- ✅ In-memory payment storage
- ✅ Comprehensive test coverage (89 tests)
- ✅ Swagger/OpenAPI documentation
- ✅ Health check endpoint (`/health`)

## Project Structure

```
src/PaymentGateway.Api/
├── Controllers/     # API endpoints
├── Services/        # Business logic
├── Interfaces/      # Service contracts
├── Models/          # DTOs and domain models
├── Helpers/         # Utility classes
└── Mappings/        # DTO transformations

test/PaymentGateway.Api.Tests/
├── Unit tests       # Component testing
└── Integration tests # End-to-end testing
```

## Configuration

Bank simulator URL can be configured in `appsettings.json`:

```json
{
  "BankSimulator": {
    "BaseUrl": "http://localhost:8080"
  }
}
```

## Bank Simulator Behavior

The bank simulator (Mountebank) uses the last digit of the card number to determine the payment response:

- **Card ends in odd digit (1, 3, 5, 7, 9)** → `Authorized` ✅
- **Card ends in even digit (2, 4, 6, 8)** → `Declined` ❌
- **Card ends in 0** → `503 Service Unavailable` 💥

**Examples:**
- `1234567890123457` (ends in 7) → Authorized
- `1234567890123456` (ends in 6) → Declined
- `1234567890123450` (ends in 0) → Service Unavailable
