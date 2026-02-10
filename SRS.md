# Software Requirements Specification (SRS)

## 1. Introduction

### 1.1 Purpose
This document defines the requirements for the shared expenses backend system and serves as the authoritative specification for scope, behavior, and constraints.

### 1.2 Scope
The system provides backend services for shared expense management in groups, including authentication (standard & social), group membership, expense tracking (shared & personal), balances, settlements, currency handling, insights, debt simplification, notifications, and activity logging.

### 1.3 Definitions and Abbreviations
- `EGP`: Egyptian Pound
- `JWT`: JSON Web Token
- `EF Core`: Entity Framework Core
- `UoW`: Unit of Work
- `Split`: A user’s share of an expense
- `Settlement`: A payment to reduce outstanding balances

## 2. System Overview

### 2.1 System Description
A multi-tenant backend system where authenticated users can create or join groups, record expenses with equal or custom splits, calculate balances, track settlements, view insights, and receive notifications.

### 2.2 High-Level Architecture Overview
Clean Architecture with layered separation:
- API layer for controllers and authorization.
- Application layer for use-case orchestration and validation.
- Domain layer for entities and business rules.
- Infrastructure layer for persistence, repositories, and external services.

## 3. Functional Requirements

### 3.1 Authentication & Authorization
1. Users can register and log in using JWT-based authentication.
2. Users can authenticate via social providers (Google, Facebook).
3. All protected endpoints require a valid JWT.
4. Role-based access applies to group administration operations.

### 3.2 User Management
1. Users can retrieve their profile information using authenticated context.
2. User identities are unique by email.

### 3.3 Group Management
1. Users can create groups and receive invite codes.
2. Users can join groups using invite codes.
3. Admins can update member roles and remove members.
4. Members can list groups and group details.

### 3.4 Expense Management
1. Members can create, update, delete, and list expenses in their groups.
2. Expenses include payer, amount, description, date, category, currency, and optional exchange-rate snapshot.
3. Categories used must belong to the group.
4. Partial updates are supported for selected fields (description, category, date).

### 3.5 Split Rules and Validation
1. Equal split distributes expense amount across all group members, with rounding tolerance.
2. Custom split requires each split user to be a member of the group.
3. Split total must equal the expense amount (within tolerance).

### 3.6 Balance Calculation Logic
1. Balances are derived from total paid and total shared amounts.
2. Settlements adjust balances (payer balance increases; payee balance decreases).
3. Balances reflect all expenses and settlements.

### 3.7 Settlement Rules
1. Settlements are only allowed between group members.
2. Settlement amount must be positive.
3. Over‑settlement is blocked based on debtor balance.

### 3.8 Currency Handling
1. Default currency is `EGP` when not provided.
2. Non‑EGP currency requires an exchange‑rate snapshot.
3. Exchange‑rate snapshot is immutable once stored.

### 3.9 Activity Logging
1. Expense, settlement, group membership, and category changes are logged.
2. Activity logs are committed atomically with the business operation.

### 3.10 Debt Simplification
1. Users can view a simplified debt plan that minimizes the total number of transactions required to settle balances.
2. Simplification is calculated dynamically based on current balances.
3. Simplification respects currency boundaries (debts in different currencies are not merged).

### 3.11 Insights & Analytics
1. Users can view total spending and category breakdowns for a group.
2. Insights support period filters (month, year, all).
3. Aggregations are grouped by currency and do not mix currencies.
4. Aggregations respect stored exchange-rate snapshots.

### 3.12 Notifications
1. Notifications are persisted for auditing and later viewing.
2. Real-time delivery is handled via WebSockets to online users.
3. System ensures delivery reliability by persisting before broadcasting.

### 3.13 Personal Expenses
1. Users can record private expenses not shared with any group.
2. Personal expenses have no splits and affect no balances.
3. Only the creating user can view or edit their personal expenses.

### 3.14 Home Feed
1. Users can view a unified feed of all relevant activities (Group Expenses, Settlements, Personal Expenses).
2. The feed supports pagination and filtering.
3. Feed items include directionality (In/Out/Neutral) to indicate financial impact on the user.

## 4. Non‑Functional Requirements

### 4.1 Performance
1. Read-heavy balance and insights queries are optimized with proper indexing.
2. Standard list endpoints respond within normal API latencies under expected load.

### 4.2 Security
1. JWT validation enforces issuer, audience, signature, and expiry.
2. Unauthorized access returns 401/403 responses.

### 4.3 Reliability & Transactional Guarantees
1. Write operations are transactional.
2. Activity logs are persisted in the same unit of work.
3. Notification persistence is performed before any real-time delivery.

### 4.4 Maintainability
1. Clean Architecture boundaries are preserved.
2. FluentValidation is applied to request DTOs.
3. Service + Repository pattern is consistently used.

## 5. Business Rules

### 5.1 Expense Validation Rules
1. Amount must be greater than 0.
2. Expense date must not be in the future beyond tolerance.
3. Category must belong to the group.

### 5.2 Split Validation Rules
1. If splits are provided, their sum equals the expense amount (within tolerance).
2. Split users must be group members.

### 5.3 Settlement Rules
1. Payer and payee must be distinct and members of the group.
2. Amount must be positive.
3. Settlement amount cannot exceed the payer’s outstanding debt.

### 5.4 Currency Rules
1. Default currency is `EGP` if not specified.
2. Non‑EGP currency requires an exchange rate snapshot.

### 5.5 Debt Simplification Rules
1. Simplification must preserve each user's net balance within each currency.
2. Simplification ignores balances within a small tolerance to avoid micro-transactions.
3. The generated plan is read-only and does not automatically execute settlements.

## 6. Assumptions & Constraints
- Backend-only system.
- Single SQL Server database.
- No external payment integrations.
- No full CQRS.

## 7. Out of Scope
- Frontend or UI concerns.
- Payment processing or banking integrations.
- Multi-database or multi-region setups.
