# Plan of Action - Payment Gateway Challenge

## What We're Building

A payment gateway API with two main features:
1. **Process payments** - Validate requests, call bank simulator, return status
2. **Retrieve payment details** - Get payment info by ID

### The Flow
1. Merchant sends payment request → We validate it
2. If invalid → Return `Rejected` (don't even call the bank)
3. If valid → Call bank simulator → Get `Authorized` or `Declined`
4. Store payment with masked card (last 4 digits only)

### Key Rules
- **Card number**: 14-19 digits, numbers only
- **Expiry**: Month 1-12, year must be in future (check month+year combo)
- **Currency**: 3 chars, we support GBP, EUR, USD
- **Amount**: Positive integer (minor units, so $10.50 = 1050)
- **CVV**: 3-4 digits, numbers only

### Bank Simulator Checks
- Card ends in odd (1,3,5,7,9) → Authorized ✅
- Card ends in even (2,4,6,8) → Declined ❌
- Card ends in 0 → 503 Service Unavailable 💥
- Missing fields → 400 Bad Request

---

## Our Assumptions

**What we're doing:**
- Only store payments that made it to the bank (Authorized/Declined). Rejected ones don't get stored.
- Mask card numbers immediately - only store last 4 digits
- If bank returns 503, treat it as a system error (don't store, return error)
- Keep it simple but extensible - don't over-engineer for hypothetcal futures

**What we're NOT doing:**
- Authentication (out of scope)
- Real database (in-memory is fine per requirements)
- Complex patterns like CQRS/MediatR (overkill for 2 endpoints)

---

## Design Decisions

**Architecture**: Simple layered service pattern (Controller → Service → Validator/BankClient → Repository)

**Why?** It's straightforward, testable, and easy to understand. No need for MediatR or CQRS when we have 2 endpoints.

**Validation**: Custom `PaymentValidator` class (no FluentValidation)

**Why?** No external dependencies, full control, easy to test and review.

**Mapping**: Manual mapping (no Mapster/AutoMapper)

**Why?** Small scope, explicit is better than magic, easy to debug.

**Testing**: Unit tests + Integration tests

**Why?** Cover validation/business logic with units, test real bank calls with integration tests.

---

## Architecture

```
Controller → Service → Validator/BankClient → Repository
```

**Controller**: Just handles HTTP requests, delegates to service  
**Service**: Orchestrates the whole flow (validate → bank → store)  
**Validator**: Pure validation logic, returns errors  
**BankClient**: Talks to bank simulator, handles HTTP  
**Repository**: Simple in-memory storage

---

## Tech Stack

- **Framework**: ASP.NET Core 8.0 (provided)
- **Validation**: Custom class (no dependencies)
- **HTTP**: Built-in HttpClient
- **Testing**: xUnit (provided)
- **Storage**: In-memory List

**No external dependencies needed** - keeping it simple!

---

## Watch Out For

**Security:**
- Never store full card numbers (mask immediately!)
- Never log sensitive data (card numbers, CVV)
- Never expose full card numbers in responses

**Checks:**
- Bank expects expiry as "MM/YYYY" format (not seperate fields)
- Expiry validation must check month+year combo is in future
- Card numbers can be 14-19 digits (not just 16!)
- Handle bank 503 errors gracefully
- Test edge cases: expired cards, invalid formats, bank failures. Make sure we recieve proper error messages too.

---

## Tasks


### Task 1: Models & Validation ✅
**Branch**: `task/1-update-models-validation`

- [x] Update `PostPaymentRequest`: `CardNumber` (string), `Cvv` (string), add data annotations
  - **Note**: CardNumber must be string to handle full 14-19 digit numbers (too large for int, and bank simulator expects string). CVV must be string to preserve leading zeros (e.g., "012") and validate length properly.
- [x] Create constants for currencies and card details.
- [x] Create `ValidationResult` class (IsValid, Errors list, factory methods)
- [x] Create `PaymentValidator` with all validation rules:
  - [x] Card number (14-19 digits, numeric)
  - [x] Expiry month (1-12)
  - [x] Expiry year (future date with month)
  - [x] Currency (GBP/EUR/USD)
  - [x] Amount (positive integer)
  - [x] CVV (3-4 digits, numeric)
- [x] Write unit tests for validator (all rules, edge cases, multiple failures)

---

### Task 2: Bank Client ✅
**Branch**: `task/2-bank-client`

- [x] Create `BankClient` class with `ProcessPaymentAsync`:
  - [x] POST to `http://localhost:8080/payments` (configurable base URL from appsettings.json)
  - [x] Handle authorized/declined responses
  - [x] Handle errors (400, 503, timeouts, invalid JSON)
  - [x] Read response content as string first (can only be read once), then deserialize
- [x] Create `BankPaymentRequest` and `BankPaymentResponse` DTOs (in `Models/Bank/` folder with JsonPropertyName attributes)
- [x] Create `IBankClient` interface in `Interfaces/` folder
- [x] Register `IBankClient` with `BankClient` implementation in DI (with HttpClient, base URL from config)
- [x] Add debug-level logging to `BankClient`
- [x] Write unit tests (mock HttpClient, test all response scenarios)

---

### Task 3: Payment Service ✅
**Branch**: `task/3-payment-service`

- [x] Create `PaymentService`:
  - [x] Inject bank client, repository (validator is static, no DI needed)
  - [x] `ProcessPaymentAsync`: validate → mask card → call bank → store → return
  - [x] Helper `MaskCardNumber` moved to `PaymentHelper` static class in `Helpers/` folder
  - [x] Mapping logic moved to `PaymentMappings` static class in `Mappings/` folder
- [x] Create `IPaymentService` interface in `Interfaces/` folder
- [x] Register `IPaymentService` with `PaymentService` implementation in DI
- [x] Add debug-level logging to `PaymentService` (respects appsettings.json log levels, structured logging)
- [x] Update `PaymentsRepository` to use `IPaymentsRepository` interface with async methods (`AddAsync`, `GetAsync`)
- [x] Add safeguards to repository: null check in `AddAsync`, early return for `Guid.Empty` in `GetAsync`
- [x] Write unit tests (mock all deps, test rejected/authorized/declined/error scenarios)

---

### Task 4: Controller Updates ✅
**Branch**: `task/4-update-controller`

- [x] Update controller: inject `IPaymentService` for POST endpoint (keep `IPaymentsRepository` for GET endpoint)
- [x] Add POST endpoint:
  - [x] Call service, return 200 OK for payment results (Authorized/Declined status in response body)
  - [x] Return 400 Bad Request with validation errors when request is invalid (created `ValidationException` class)
  - [x] Return 503 Service Unavailable when bank returns 503 or connection errors occur
  - [x] Return 500 Internal Server Error for other bank errors (timeouts, invalid responses, etc.)
- [x] GET endpoint already returns 404 if not found (fixed in Task 3)
- [x] Update `BankClient` to handle connection errors:
  - [x] Catch `SocketException` directly (connection refused)
  - [x] Detect connection errors in `HttpRequestException` via `IsConnectionError` helper method
  - [x] Map connection errors to "Bank service is unavailable" with 503 status
- [x] Update `MaskCardNumber` to return `string` instead of `int` to preserve leading zeros
- [x] Update response models (`PostPaymentResponse`, `GetPaymentResponse`) to use `string` for `CardNumberLastFour` and `Status`
- [x] Write unit tests for `PaymentHelper` class (8 tests covering valid/invalid cases, leading zeros, edge cases)
- [x] Write integration tests:
  - [x] POST authorized (returns 200 with Authorized status)
  - [x] POST declined (returns 200 with Declined status)
  - [x] POST invalid request (returns 400 with validation errors, bank not called)
  - [x] POST bank service unavailable (returns 503)
  - [x] GET found (returns 200)
  - [x] GET not found (returns 404)

---

### Task 5: Integration Tests & Edge Cases ✅
**Branch**: `task/5-integration-tests`

- [x] Created `PaymentGatewayIntegrationTests` class focused on integration concerns (not duplicating unit tests)
- [x] Test end-to-end flow:
  - [x] Valid request returns 200 OK with payment response
  - [x] Invalid request returns 400 Bad Request with validation errors
- [x] Test response structure matches assessment requirements:
  - [x] Status as string ("Authorized" or "Declined")
  - [x] Last four card digits (preserves leading zeros, verified as string type)
  - [x] All required fields (Id, Status, CardNumberLastFour, ExpiryMonth, ExpiryYear, Currency, Amount)
- [x] Test bank simulator behavior (odd/even/0 card endings):
  - [x] Odd endings (1,3,5,7,9) → "Authorized"
  - [x] Even endings (2,4,6,8) → "Declined"
  - [x] Zero ending → 503 Service Unavailable
- [x] Test integration-specific edge cases:
  - [x] Card boundary lengths (14 and 19 digits) work through full flow
  - [x] CVV with leading zeros preserved through full flow
- [x] Test GET endpoint:
  - [x] Returns same fields as POST response
  - [x] Returns 404 for non-existent payments
  - [x] Status is string type
- [x] Created `TestHelpers` class to share test data creation methods across all test classes
- [x] Refactored integration tests to remove duplication (detailed validation tests remain in `PaymentValidatorTests`)
- [x] Removed regions and summary comments for cleaner code
- [x] All 27 integration tests pass (focused on integration, not validation details)

---

### Task 6: Polish & Docs
**Branch**: `task/6-code-quality-docs`

- [ ] Code cleanup: remove unused code, add XML comments, fix warnings
- [ ] Update README: setup, running, testing, API docs
- [ ] Add appsettings config (bank URL, logging)
- [ ] Final checks: all tests pass, Swagger works, no sensitive data in logs. Double check that we're not logging any card numbers!

---

## Success Criteria

**Must have:**
- ✅ POST/GET endpoints work correctly
- ✅ All validation rules implemented
- ✅ Bank simulator integration works
- ✅ Card numbers masked (never exposed)
- ✅ Proper HTTP status codes
- ✅ Good test coverage
- ✅ Code compiles and runs

**Nice to have:**
- ✅ Clear error messages
- ✅ Clean, readable code
- ✅ Good documentation

