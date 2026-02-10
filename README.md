# Shared Expenses Backend – Case Study

## Project Overview
This system solves a real-world problem: tracking shared expenses in groups with financial correctness and accountability. In this domain, small errors compound quickly—incorrect splits, missing settlements, or inconsistent balances undermine trust. This backend was designed to be precise, auditable, and production-ready, with a strong architectural foundation that keeps business rules explicit and enforceable.

## Why This Project Exists
Shared expenses are deceptively complex. Users expect instant, correct balances and clear accountability across multiple participants. Naive CRUD-only designs fail because they don’t handle split rules, rounding, balance reconciliation, or settlement constraints. This system is different because it encodes the financial model explicitly, validates inputs aggressively, and guarantees auditability and consistency across the entire flow.

## Architecture Overview
This project follows Clean Architecture with clear layer boundaries:
- API: HTTP controllers, auth, request/response shaping.
- Application: use cases, validation, and orchestration.
- Domain: entities and core rules.
- Infrastructure: persistence, repositories, JWT, EF Core.

We use Service + Repository rather than full CQRS to keep the design pragmatic while still cleanly separating read/write concerns. This avoids unnecessary complexity while preserving testability and maintainability.

## Core Business Logic
- Expense Splitting: Supports equal splits (with rounding tolerance) and custom amount splits. Custom splits require membership and must sum to the total amount.
- Balance Calculation: Balances are derived from total paid vs total shared, with settlements applied to adjust net position.
- Settlement Rules: Settlements are validated for membership, positive amount, and over‑settlement protection based on debtor balance.
- Debt Simplification: Minimizes the number of transactions needed to settle up using a greedy algorithm. Calculates simplified transfers per currency without altering net balances.
- Currency Handling: Default currency is `EGP`. Non‑EGP expenses require an exchange‑rate snapshot that is persisted and treated as immutable.
- Personal Expenses: Private expense tracking for individual users, isolated from groups but integrated into the unified feed.
- Unified Home Feed: Aggregates group expenses, settlements, and personal expenses into a single chronological timeline with directionality (In/Out/Neutral).
- Real-time Notifications: Delivers instant updates to online users via WebSockets for critical events (Expense Added, Settlement Created, etc.).

## Data Consistency & Safety
- Transactions and Unit of Work: All write operations are coordinated through a unit of work to ensure consistency.
- Atomic Activity Logging: Activity logs are committed with the same transaction as the business operation.
- Validation Strategy: FluentValidation enforces DTO integrity before service execution, including cross-field rules.
- Invalid State Prevention: Strict checks prevent split mismatches, unauthorized access, and over‑settlement.

## API Design
The API is organized around domain features and enforces authorization at every boundary:
- Auth: JWT-based registration and login.
- Groups: group creation, invites, roles, and membership management.
- Expenses: create/update/delete, equal or custom splits, categories, currency support.
- Balances: real-time balance computation for each group.
- Settlements: debt reduction with over‑settlement protection.
- Activity Logs: audit trails for key domain actions.

## Quality & Testing
The system includes unit and integration tests across core flows:
- Validation coverage for DTOs and cross-field rules.
- Expense, balance, and settlement lifecycle tests.
- Security tests for membership and role enforcement.

## Tech Stack
- ASP.NET Core Web API
- C#
- EF Core + SQL Server
- JWT Authentication + Social Auth (Google/Facebook)
- Native WebSockets (Real-time)
- FluentValidation
- Clean Architecture
- Service + Repository pattern
- No full CQRS

## Case Study Outcomes
- Financial correctness through explicit domain rules.
- Consistency guarantees with transactional boundaries.
- Auditability with detailed activity logging.
- Maintainable architecture that scales with business complexity.
