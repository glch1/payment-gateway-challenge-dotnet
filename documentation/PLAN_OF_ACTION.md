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

### Task 4: Controller Updates
**Branch**: `task/4-update-controller`

- [ ] Update controller: inject `PaymentService` (not repository)
- [ ] Add POST endpoint:
  - [ ] Call service, return 200/400/503 with appropriate responses
- [ ] Fix GET endpoint: return 404 if not found
- [ ] Write integration tests (POST authorized/declined/rejected, GET found/not found)

---

### Task 5: Integration Tests & Edge Cases
**Branch**: `task/5-integration-tests`

- [ ] Set up integration test project (bank simulator config)
- [ ] Test full flow with real bank (odd/even/0 endings)
- [ ] Test edge cases: 14/19 digit cards, expiry boundaries, min amount, CVV lengths
- [ ] Test error scenarios: bank down, timeouts, invalid JSON

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

