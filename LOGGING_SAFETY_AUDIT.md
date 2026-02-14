# Logging Safety Audit (Sensitive Data)

## Scope
- Serilog host logging (console + rolling file) and request logging.
- API exception logging and application logs emitted via `ILogger<T>`.
- Goal: prevent secrets/PII/raw payloads from being emitted to logs.

## What Was Reviewed
- Host + Serilog wiring: `Expense.API/Program.cs`
- Request logging enrichment: `Expense.API/Extensions/StartupExtensions/ApplicationMiddlewareExtensions.cs`
- Exception logging: `Expense.API/Middlewares/GlobalExceptionHandlerMiddleware.cs`
- Business/service logs:
  - `Expense.Infrastructure/Expenses/ExpenseService.cs`
  - `Expense.Infrastructure/Settlements/SettlementService.cs`
  - `Expense.Infrastructure/Groups/GroupService.cs`
  - `Expense.Infrastructure/Notifications/NativeWebSocketNotifier.cs`
  - WebSocket middleware (API pipeline): `Expense.API/Middlewares/WebSocketMiddleware.cs`

## Findings
### Safe (No Secrets / No Raw Payloads)
- Request logging does not log request bodies or headers (including Authorization).
- Service logs use stable identifiers and operational metrics (ids, counts, amounts) and do not log DTOs/entities.
- No `{ @Obj }` destructuring patterns found in logging templates.

### Risk Areas and Mitigations
- Configuration contains secrets:
  - `Expense.API/appsettings.json` includes JwtSettings and Cloudinary credentials.
  - Mitigation: no configuration values are emitted by logging code; do not add configuration-dumping logs.
- Exception messages can include user input or secret-like values (depending on upstream libraries).
  - Mitigation applied: exception logging was updated to avoid logging exception messages for expected/typed failures and to avoid logging full exception details in non-development environments.

## Applied Hardening Changes
- Updated `GlobalExceptionHandlerMiddleware` to:
  - Avoid logging `{Message}` for typed/expected exceptions.
  - Avoid emitting exception objects in non-development environments for external/database exceptions and generic unhandled exceptions.
  - Keep TraceId correlation on all error paths.

## Non-Logging PII Notes (Out of Serilog Scope)
- `GroupService` uses email in activity log details and notification payloads for member-add flows.
  - These are not written to Serilog logs by current code paths, but they are still PII stored/transmitted within the application.

## Verification
- Build: `dotnet build Expense.API/Expense.API.csproj -c Release`
- Unit tests: `dotnet test Expense.UnitTests/Expense.UnitTests.csproj -c Release`
- Integration tests: `dotnet test Expense.IntegrationTests/Expense.IntegrationTests.csproj -c Release`

