# Logging Design Checklist (Serilog)

## Goals
- Produce structured, searchable operational logs.
- Ensure logs are production-safe (no secrets, no raw payloads).
- Correlate logs across request, service, and middleware via TraceId.

## Event Model
### Event Name Convention
- Use `Domain.Area.Action` (stable, no spaces), e.g.:
  - `Request.Completed`
  - `Request.Failed`
  - `Auth.Login.Succeeded`
  - `Auth.Login.Failed`
  - `Group.Created`
  - `Group.MemberAdded`
  - `Expense.Created`
  - `Expense.Updated`
  - `Expense.Deleted`
  - `Settlement.Created`
  - `Settlement.Deleted`
  - `Notifications.WebSocket.Connected`
  - `Notifications.WebSocket.Disconnected`

### Standard Properties (Prefer Consistent Keys)
- Correlation:
  - `TraceId`
  - `RequestId` (if available)
- Actor/Identity:
  - `ActorUserId` (preferred)
  - `UserId` (fallback when actor semantics are unclear)
- Route/Request:
  - `Path`
  - `Method`
  - `StatusCode`
  - `ElapsedMs`
- Domain Identifiers:
  - `GroupId`
  - `ExpenseId`
  - `SettlementId`
  - `CategoryId`
  - `NotificationId`
- Context:
  - `EnvironmentName`
  - `MachineName`
  - `ThreadId`

## Log Level Guidance
- Debug:
  - High-volume diagnostics (query decisions, counts, cache hits), not enabled by default in Production.
- Information:
  - Business lifecycle events (created/updated/deleted), request completion.
- Warning:
  - Recoverable/expected failures (validation errors, access denied, not found, notifier failures).
- Error:
  - Request failure (5xx), unhandled or unexpected exceptions, dependency failures.
- Critical:
  - Host termination, corruption, startup failures.

## Redaction & Exclusion Rules (Must Not Log)
### Secrets (Never)
- JWT signing keys, refresh tokens, access tokens.
- Authorization headers, bearer tokens, cookies.
- API secrets (e.g., Cloudinary ApiSecret), connection strings containing passwords.
- User secrets / environment variables containing secrets.

### PII (Avoid; If Necessary, Use Minimization)
- Email, phone, full name, address.
- If needed for support, log only:
  - UserId
  - Domain identifiers (GroupId/ExpenseId/etc.)
  - Counts (e.g., number of splits)

### Payloads (Never)
- Raw request/response bodies.
- Full DTOs or entity objects (including destructuring).
- File contents, uploaded metadata beyond safe type/size.

### Exception Content (Minimize)
- Prefer logging exception type + stable IDs.
- If logging exception objects, ensure downstream sinks do not ship logs to untrusted destinations, and avoid logging exception messages that include user input or secrets.

## Review Checklist (Before Merging)
- Structured templates:
  - Uses named placeholders (no string concatenation).
  - Uses stable IDs (GroupId, ExpenseId, ActorUserId) instead of PII.
- Safety:
  - No secrets/headers/tokens/body in templates or properties.
  - No `{Dto}` / destructuring (`{@...}`) of payload objects.
- Volume:
  - High-volume events are `Debug` or sampled.
  - Request logging is enabled and has correlation fields.
- Correlation:
  - Request logs include `TraceId`.
  - Service logs include `GroupId` and relevant domain ids when available.

