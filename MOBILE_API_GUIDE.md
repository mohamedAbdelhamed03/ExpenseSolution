# Expense Solution — Mobile Integration Guide

> **Version:** 1.0  
> **Last updated:** 2026-06-14  
> **Base URL (production):** `https://<your-domain>/api`  
> **Base URL (local dev):** `http://localhost:5000/api`

---

## Table of Contents

1. [Overview](#1-overview)
2. [Authentication](#2-authentication)
3. [Standard Response Format](#3-standard-response-format)
4. [Common Headers](#4-common-headers)
5. [Error Handling](#5-error-handling)
6. [Typical App Flows](#6-typical-app-flows)
7. [API Reference](#7-api-reference)
   - [Auth](#71-auth)
   - [Home Feed](#72-home-feed)
   - [Groups](#73-groups)
   - [Expenses](#74-expenses)
   - [Personal Expenses](#75-personal-expenses)
   - [Balances](#76-balances)
   - [Settlements](#77-settlements)
   - [Debts (Simplified)](#78-debts-simplified)
   - [Insights](#79-insights)
   - [Categories](#710-categories)
   - [Activity Logs](#711-activity-logs)
   - [Notifications](#712-notifications)
   - [Files (Upload)](#713-files-upload)
8. [Real-time WebSocket](#8-real-time-websocket)
9. [Business Rules Summary](#9-business-rules-summary)
10. [Currency Notes](#10-currency-notes)

---

## 1. Overview

This is a **Shared Expenses Management** platform. Users create groups, add expenses, split costs, track balances, and settle debts. They can also track private personal expenses.

**Core capabilities for mobile:**
- Register / login (standard + Google / Facebook)
- Create and join expense groups
- Add expenses with equal or custom splits
- View real-time balances (who owes whom)
- Settle debts between members
- View a unified home feed of all activity
- Track private personal expenses
- Receive real-time push notifications via WebSocket
- View spending insights by category and period

---

## 2. Authentication

The API uses **JWT Bearer tokens**.

### Token Storage
Store two tokens securely (e.g., Keychain on iOS, EncryptedSharedPreferences on Android):

| Token | Purpose | Lifetime |
|-------|---------|---------|
| `accessToken` | Sent on every request | ~1 hour |
| `refreshToken` | Used to get a new access token silently | 7 days |

### Token Usage
Add the access token to every protected request:
```
Authorization: Bearer <accessToken>
```

### Token Refresh Flow
When any request returns **401 Unauthorized**:
1. Call `POST /api/auth/refresh` with the stored `refreshToken`
2. Store the new `accessToken` and `refreshToken`
3. Retry the original request with the new `accessToken`
4. If refresh also returns 401, force the user to log in again

### Token Revocation
Logout increments a server-side token version, immediately invalidating all existing tokens. Always call logout when the user signs out.

---

## 3. Standard Response Format

Every API response is wrapped in this envelope:

```json
{
  "success": true,
  "message": "Optional message",
  "data": { },
  "errors": [],
  "statusCode": 200,
  "timestamp": "2026-06-14T10:00:00Z",
  "traceId": "00-abc123def456..."
}
```

| Field | Type | Description |
|-------|------|-------------|
| `success` | bool | `true` if the operation succeeded |
| `message` | string? | Human-readable message (may be null) |
| `data` | T? | The response payload (null on error) |
| `errors` | string[] | Validation or error messages (empty on success) |
| `statusCode` | int | HTTP status code mirrored here |
| `timestamp` | string | ISO 8601 UTC timestamp |
| `traceId` | string | For debugging — include in bug reports |

**Always check `success` first, then read `data`.**

---

## 4. Common Headers

```
Authorization: Bearer <accessToken>      (required on protected endpoints)
Content-Type: application/json           (required on POST/PUT/PATCH)
Accept-Language: en                      (optional, for localized messages)
```

---

## 5. Error Handling

| HTTP Status | Meaning | Action |
|-------------|---------|--------|
| 400 | Validation failed | Show `errors[]` to the user |
| 401 | Unauthenticated / token expired | Refresh token, then retry |
| 403 | Forbidden (wrong role) | Show "not allowed" message |
| 404 | Resource not found | Show "not found" message |
| 409 | Conflict (e.g. email already registered) | Show specific error |
| 500 | Server error | Show generic error, log `traceId` |

**Example 400 response:**
```json
{
  "success": false,
  "message": null,
  "data": null,
  "errors": ["Password must be at least 6 characters", "Email is required"],
  "statusCode": 400,
  "timestamp": "2026-06-14T10:00:00Z",
  "traceId": "00-abc123..."
}
```

---

## 6. Typical App Flows

### 6.1 Onboarding — New User
```
POST /api/auth/register
  → Store userId
POST /api/auth/login
  → Store accessToken, refreshToken
GET  /api/home          ← home feed (empty)
GET  /api/groups        ← user's groups (empty)
```

### 6.2 Social Login (Google / Facebook)
```
[Get OAuth token from Google/Facebook SDK]
POST /api/auth/google   OR   POST /api/auth/facebook
  → Store accessToken, refreshToken (same as regular login)
```

### 6.3 Create a Group and Add Members
```
POST /api/groups                      ← create group, receive groupId + inviteCode
POST /api/groups/{groupId}/members    ← add member by email (admin only)
  OR share inviteCode so members can:
POST /api/groups/join/{inviteCode}    ← member joins themselves
```

### 6.4 Add a Group Expense
```
GET  /api/groups/{groupId}/categories   ← list available categories
POST /api/groups/{groupId}/expenses     ← create expense (equal split or custom)
GET  /api/groups/{groupId}/balances     ← updated balances
```

### 6.5 Settle a Debt
```
GET  /api/groups/{groupId}/debts/simplified   ← see who owes whom
POST /api/groups/{groupId}/settlements        ← record the payment
GET  /api/groups/{groupId}/balances           ← confirm updated balances
```

### 6.6 Home Screen
```
GET /api/home                  ← unified feed (expenses + settlements + personal)
GET /api/insights/home         ← spending summary across all groups
GET /api/notifications/unread  ← notification badge count
```

### 6.7 Token Refresh (Silent)
```
[On 401 from any request]
POST /api/auth/refresh  { "refreshToken": "..." }
  → new accessToken + refreshToken
[Retry original request]
```

---

## 7. API Reference

### 7.1 Auth

#### `POST /api/auth/register`
Register a new account.

**Auth required:** No

**Request:**
```json
{
  "email": "user@example.com",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "firstName": "Sara",
  "lastName": "Ahmed",
  "phoneNumber": "+201234567890"
}
```

**Validation:**
- `email`: required, valid format, unique
- `password`: min 6 chars, must contain uppercase + lowercase + digit
- `confirmPassword`: must match `password`
- `firstName`, `lastName`: required

**Success (200):**
```json
{
  "success": true,
  "data": {
    "success": true,
    "message": "Registration successful",
    "userId": "b5f5d9b8-0c7d-4e1d-9f28-5bfa2f5ed0a1"
  }
}
```

**Errors:** 400 (validation), 409 (email already registered)

---

#### `POST /api/auth/login`
Login with email and password.

**Auth required:** No

**Request:**
```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Success (200):**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "b1c2d3e4f5...",
    "expiresAt": "2026-06-14T11:00:00Z",
    "userId": "b5f5d9b8-0c7d-4e1d-9f28-5bfa2f5ed0a1",
    "email": "user@example.com",
    "roles": ["User"]
  }
}
```

**Errors:** 401 (invalid credentials)

---

#### `POST /api/auth/google`
Login or register via Google OAuth.

**Auth required:** No

**Request:**
```json
{
  "token": "<google_id_token>",
  "provider": "Google"
}
```

**Success:** Same as login response above.

---

#### `POST /api/auth/facebook`
Login or register via Facebook OAuth.

**Auth required:** No

**Request:**
```json
{
  "token": "<facebook_access_token>",
  "provider": "Facebook"
}
```

**Success:** Same as login response above.

---

#### `POST /api/auth/refresh`
Get new tokens using a refresh token.

**Auth required:** No

**Request:**
```json
{
  "refreshToken": "b1c2d3e4f5..."
}
```

**Success (200):**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "new_refresh_token...",
    "expiresAt": "2026-06-14T12:00:00Z"
  }
}
```

**Errors:** 401 (invalid or expired refresh token → user must log in again)

---

#### `POST /api/auth/logout`
Invalidate the current access token server-side.

**Auth required:** Yes

**Request:** No body

**Success (200):**
```json
{ "success": true, "data": {} }
```

---

#### `POST /api/auth/revoke-refresh-token`
Revoke a specific refresh token (e.g. when signing out of another device).

**Auth required:** Yes

**Request body:** Plain string (the refresh token to revoke)
```json
"b1c2d3e4f5..."
```

---

#### `POST /api/auth/change-password`
Change the authenticated user's password.

**Auth required:** Yes

**Request:**
```json
{
  "currentPassword": "OldPass123!",
  "newPassword": "NewPass456!",
  "confirmNewPassword": "NewPass456!"
}
```

---

#### `POST /api/auth/forgot-password`
Send password reset email.

**Auth required:** No

**Request:**
```json
{ "email": "user@example.com" }
```

**Note:** Always returns 200 (for security — does not reveal if email exists).

---

#### `POST /api/auth/reset-password`
Reset password using token from email.

**Auth required:** No

**Request:**
```json
{
  "email": "user@example.com",
  "token": "<reset_token_from_email>",
  "newPassword": "NewPass456!",
  "confirmNewPassword": "NewPass456!"
}
```

---

#### `GET /api/auth/me`
Get the current authenticated user's info.

**Auth required:** Yes

**Success (200):**
```json
{
  "success": true,
  "data": {
    "userId": "b5f5d9b8-...",
    "email": "user@example.com",
    "roles": ["User"]
  }
}
```

---

### 7.2 Home Feed

#### `GET /api/home`
Unified, chronological feed of all activity relevant to the current user: group expenses, settlements, and personal expenses.

**Auth required:** Yes

**Query params:**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 10 | Items per page |

**Success (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "type": "Expense",
      "amount": 150.00,
      "currency": "EGP",
      "date": "2026-06-14T10:00:00Z",
      "description": "Dinner",
      "groupId": "uuid",
      "groupName": "Trip to Sinai",
      "otherUserId": "uuid",
      "otherUserName": "Omar Hassan",
      "direction": "Out"
    },
    {
      "id": "uuid",
      "type": "Settlement",
      "amount": 75.00,
      "currency": "EGP",
      "date": "2026-06-14T12:00:00Z",
      "description": null,
      "groupId": "uuid",
      "groupName": "Trip to Sinai",
      "otherUserId": "uuid",
      "otherUserName": "Omar Hassan",
      "direction": "In"
    },
    {
      "id": "uuid",
      "type": "PersonalExpense",
      "amount": 30.00,
      "currency": "EGP",
      "date": "2026-06-13T08:00:00Z",
      "description": "Coffee",
      "groupId": null,
      "groupName": null,
      "otherUserId": null,
      "otherUserName": null,
      "direction": "Neutral"
    }
  ]
}
```

**`direction` values:**

| Value | Meaning |
|-------|---------|
| `Out` | Money the user paid |
| `In` | Money the user received / is owed |
| `Neutral` | No cash flow (personal expense) |

**`type` values:** `Expense`, `Settlement`, `PersonalExpense`

---

### 7.3 Groups

#### `POST /api/groups`
Create a new group. The creator becomes an Admin.

**Auth required:** Yes

**Request:**
```json
{
  "name": "Trip to Sinai",
  "logoUrl": "https://res.cloudinary.com/demo/image/upload/v1/sample.jpg"
}
```

**`logoUrl`:** Optional. Upload an image first using `POST /api/files/upload`, then pass the returned URL here.

**Success (200):**
```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Trip to Sinai",
    "logoUrl": "https://...",
    "inviteCode": "ABC123XY",
    "members": [
      {
        "userId": "b5f5d9b8-...",
        "email": "user@example.com",
        "firstName": "Sara",
        "lastName": "Ahmed",
        "role": "Admin"
      }
    ]
  }
}
```

---

#### `GET /api/groups`
List all groups the current user belongs to.

**Auth required:** Yes

**Success (200):** Array of `GroupDto` (same structure as above).

---

#### `GET /api/groups/{groupId}`
Get details for a single group.

**Auth required:** Yes (must be a group member)

**Success (200):** Single `GroupDto`.

---

#### `POST /api/groups/join/{inviteCode}`
Join a group using an invite code.

**Auth required:** Yes

**URL param:** `inviteCode` — the code from `GroupDto.inviteCode`

**Request:** No body

**Success (200):** `{ "success": true, "data": {} }`

**Errors:** 404 (invalid invite code), 409 (already a member)

---

#### `POST /api/groups/{groupId}/members`
Add a member to the group by their email address.

**Auth required:** Yes (Admin only)

**Request:**
```json
{ "email": "newmember@example.com" }
```

**Success (200):** `{ "success": true, "data": true }`

**Errors:** 403 (not admin), 404 (user not found), 409 (already a member)

---

#### `PUT /api/groups/{groupId}/members/{userId}`
Update a member's role (full update).

**Auth required:** Yes (Admin only)

**Request:**
```json
{ "role": "Admin" }
```

**Roles:** `"Admin"` or `"Member"`

---

#### `PATCH /api/groups/{groupId}/members/{userId}/role`
Update a member's role (partial update).

**Auth required:** Yes (Admin only)

**Request:**
```json
{ "role": "Member" }
```

---

#### `DELETE /api/groups/{groupId}/members/{userId}`
Remove a member from the group.

**Auth required:** Yes (Admin only)

**Success (200):** `{ "success": true, "data": {} }`

---

### 7.4 Expenses

All expense endpoints are nested under a group: `/api/groups/{groupId}/expenses`

#### `POST /api/groups/{groupId}/expenses`
Add an expense to the group.

**Auth required:** Yes (must be a group member)

**Request (equal split — omit `splits`):**
```json
{
  "amount": 300.00,
  "currency": "EGP",
  "description": "Dinner at restaurant",
  "categoryId": "7f9f62d1-7c2a-4d8d-9c6f-0b2c4a8e9c11",
  "expenseDate": "2026-06-14T20:00:00Z"
}
```

**Request (custom split — include `splits`):**
```json
{
  "amount": 300.00,
  "currency": "EGP",
  "description": "Dinner at restaurant",
  "categoryId": "7f9f62d1-7c2a-4d8d-9c6f-0b2c4a8e9c11",
  "expenseDate": "2026-06-14T20:00:00Z",
  "splits": [
    { "userId": "user-id-1", "amount": 200.00 },
    { "userId": "user-id-2", "amount": 100.00 }
  ]
}
```

**Fields:**

| Field | Required | Description |
|-------|----------|-------------|
| `amount` | Yes | Total amount, must be > 0 |
| `currency` | No | 3-letter code, defaults to `"EGP"` |
| `exchangeRate` | No | Required if currency is not EGP |
| `description` | Yes | What the expense is for |
| `categoryId` | No | Must belong to this group |
| `expenseDate` | Yes | ISO 8601 datetime |
| `splits` | No | Custom split array. If omitted, equal split is applied across all members |

**Split rules:**
- If `splits` is provided: every userId must be a group member, and the sum of all amounts must equal `amount`
- If `splits` is omitted: amount is divided equally among all current members

**Success (200):**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "groupId": "uuid",
    "paidByUserId": "uuid",
    "amount": 300.00,
    "currency": "EGP",
    "exchangeRate": null,
    "description": "Dinner at restaurant",
    "categoryId": "uuid",
    "expenseDate": "2026-06-14T20:00:00Z",
    "createdAt": "2026-06-14T20:01:00Z",
    "splits": [
      { "userId": "user-id-1", "amount": 150.00 },
      { "userId": "user-id-2", "amount": 150.00 }
    ]
  }
}
```

---

#### `GET /api/groups/{groupId}/expenses`
List all expenses in the group.

**Auth required:** Yes (group member)

**Success (200):** Array of `ExpenseDto`

---

#### `GET /api/groups/{groupId}/expenses/{expenseId}`
Get a single expense by ID.

**Auth required:** Yes (group member)

**Success (200):** Single `ExpenseDto`

---

#### `PUT /api/groups/{groupId}/expenses/{expenseId}`
Update an expense (full replace).

**Auth required:** Yes (group member)

**Request:** Same fields as Create (all required)

---

#### `PATCH /api/groups/{groupId}/expenses/{expenseId}`
Update an expense partially. Only include the fields you want to change.

**Auth required:** Yes (group member)

**Request (example — only update description and date):**
```json
{
  "description": "Lunch instead",
  "expenseDate": "2026-06-14T13:00:00Z"
}
```

---

#### `DELETE /api/groups/{groupId}/expenses/{expenseId}`
Delete an expense.

**Auth required:** Yes (group member — Admin can delete any, Member can delete own)

**Success (200):**
```json
{ "success": true, "data": true }
```

---

### 7.5 Personal Expenses

Private expenses visible only to the current user. Appear in the home feed and insights.

#### `POST /api/personal-expenses`
Create a personal expense.

**Auth required:** Yes

**Request:**
```json
{
  "amount": 50.00,
  "currency": "EGP",
  "date": "2026-06-14T08:00:00Z",
  "description": "Morning coffee",
  "categoryId": "7f9f62d1-7c2a-4d8d-9c6f-0b2c4a8e9c11"
}
```

**`categoryId`:** Optional. If provided, use a category from any of the user's groups or a system category.

**Success (200):**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "userId": "uuid",
    "amount": 50.00,
    "currency": "EGP",
    "date": "2026-06-14T08:00:00Z",
    "description": "Morning coffee",
    "categoryId": "uuid",
    "categoryName": "Food",
    "categoryIcon": "🍔",
    "createdAt": "2026-06-14T08:01:00Z"
  }
}
```

---

#### `GET /api/personal-expenses`
List personal expenses (paginated).

**Auth required:** Yes

**Query params:**

| Param | Default |
|-------|---------|
| `page` | 1 |
| `pageSize` | 20 |

---

#### `GET /api/personal-expenses/{id}`
Get a single personal expense.

**Auth required:** Yes

---

#### `PUT /api/personal-expenses/{id}`
Full update of a personal expense.

**Auth required:** Yes

**Request:** Same as create (all fields required)

---

#### `PATCH /api/personal-expenses/{id}`
Partial update. Only include fields to change.

**Auth required:** Yes

**Request (example):**
```json
{ "amount": 60.00 }
```

---

#### `DELETE /api/personal-expenses/{id}`
Delete a personal expense.

**Auth required:** Yes

---

### 7.6 Balances

#### `GET /api/groups/{groupId}/balances`
Get each member's net balance in the group.

**Auth required:** Yes (group member)

**Success (200):**
```json
{
  "success": true,
  "data": [
    {
      "userId": "user-id-1",
      "totalPaid": 300.00,
      "totalShared": 100.00,
      "balance": 200.00
    },
    {
      "userId": "user-id-2",
      "totalPaid": 0.00,
      "totalShared": 200.00,
      "balance": -200.00
    }
  ]
}
```

**Balance interpretation:**

| Value | Meaning |
|-------|---------|
| Positive (e.g. `+200`) | This user is owed money |
| Negative (e.g. `-200`) | This user owes money |
| Zero | Settled up |

> **Note:** Balances are per-currency when multi-currency expenses exist.

---

### 7.7 Settlements

A settlement records that someone paid back their debt.

#### `POST /api/groups/{groupId}/settlements`
Record a settlement payment.

**Auth required:** Yes (current user is the payer)

**Request:**
```json
{
  "payeeUserId": "user-id-of-person-you-are-paying",
  "amount": 75.00,
  "currency": "EGP",
  "exchangeRate": null,
  "settlementDate": "2026-06-14T15:00:00Z"
}
```

**Rules:**
- `payeeUserId` must be a group member
- `amount` must be > 0
- Cannot settle more than what you owe (over-settlement is blocked)

**Success (200):**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "groupId": "uuid",
    "payerUserId": "uuid",
    "payeeUserId": "uuid",
    "amount": 75.00,
    "currency": "EGP",
    "exchangeRate": null,
    "settlementDate": "2026-06-14T15:00:00Z",
    "createdAt": "2026-06-14T15:01:00Z"
  }
}
```

---

#### `GET /api/groups/{groupId}/settlements`
List all settlements in the group.

**Auth required:** Yes (group member)

**Success (200):** Array of `SettlementDto`

---

#### `GET /api/groups/{groupId}/settlements/{settlementId}`
Get a single settlement.

**Auth required:** Yes (group member)

---

#### `DELETE /api/groups/{groupId}/settlements/{settlementId}`
Delete a settlement.

**Auth required:** Yes (creator only)

---

### 7.8 Debts (Simplified)

#### `GET /api/groups/{groupId}/debts/simplified`
Returns the minimum set of transfers needed to clear all debts. Use this to show users a "Settle Up" screen.

**Auth required:** Yes (group member)

**Success (200):**
```json
{
  "success": true,
  "data": [
    {
      "currency": "EGP",
      "transfers": [
        {
          "fromUserId": "user-id-A",
          "toUserId": "user-id-B",
          "amount": 150.00,
          "currency": "EGP"
        }
      ]
    }
  ]
}
```

**How to use:** Display each `transfer` as "User A should pay User B — 150 EGP". Each transfer maps directly to one `POST /settlements` call.

---

### 7.9 Insights

#### `GET /api/groups/{groupId}/insights`
Spending breakdown for a group by category.

**Auth required:** Yes (group member)

**Query params:**

| Param | Values | Description |
|-------|--------|-------------|
| `period` | `month`, `year`, `all` | Time window |
| `date` | `2026-06` or `2026` | Required for `month`/`year` period |
| `scope` | `group`, `me` | `group` = all members; `me` = current user's share only |

**Example:** `GET /api/groups/{groupId}/insights?period=month&date=2026-06&scope=me`

**Success (200):**
```json
{
  "success": true,
  "data": [
    {
      "groupId": "uuid",
      "period": "month",
      "date": "2026-06",
      "currency": "EGP",
      "totalAmount": 500.00,
      "categories": [
        {
          "categoryId": "uuid",
          "categoryName": "Food",
          "amount": 300.00,
          "percentage": 60.0,
          "currency": "EGP"
        },
        {
          "categoryId": "uuid",
          "categoryName": "Transport",
          "amount": 200.00,
          "percentage": 40.0,
          "currency": "EGP"
        }
      ]
    }
  ]
}
```

---

#### `GET /api/insights/home`
Aggregated spending insights across all groups + personal expenses for the current user.

**Auth required:** Yes

**Query params:** `period`, `date` (same as above, no `scope`)

**Success (200):** Same structure as group insights, `groupId` is null.

---

### 7.10 Categories

Categories are used to label expenses (e.g. Food, Transport, Utilities).

#### `GET /api/groups/{groupId}/categories`
List available categories for a group (includes system defaults).

**Auth required:** Yes (group member)

**Success (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "groupId": "uuid",
      "userId": null,
      "name": "Food",
      "description": "Meals and groceries",
      "icon": "🍔",
      "isSystem": true,
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

**`isSystem: true`** means this is a default category and cannot be deleted.

---

#### `POST /api/groups/{groupId}/categories`
Create a custom category for the group.

**Auth required:** Yes (Admin only)

**Request:**
```json
{
  "name": "Entertainment",
  "description": "Movies, concerts, events",
  "icon": "🎬"
}
```

---

#### `PUT /api/categories/{categoryId}`
Full update of a category.

**Auth required:** Yes (Admin only, non-system categories only)

**Request:**
```json
{
  "name": "Entertainment",
  "description": "Updated description",
  "icon": "🎭"
}
```

---

#### `PATCH /api/categories/{categoryId}`
Partial update of a category.

**Auth required:** Yes (Admin only)

**Request (example — only change icon):**
```json
{ "icon": "🚗" }
```

---

#### `DELETE /api/categories/{categoryId}`
Delete a custom category.

**Auth required:** Yes (Admin only, non-system categories only)

---

### 7.11 Activity Logs

#### `GET /api/groups/{groupId}/activities`
Paginated list of all audited actions in the group.

**Auth required:** Yes (group member)

**Query params:**

| Param | Default |
|-------|---------|
| `page` | 1 |
| `pageSize` | 20 |

**Success (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "groupId": "uuid",
      "userId": "uuid",
      "action": "ExpenseCreated",
      "entityType": "Expense",
      "entityId": "uuid",
      "details": { "amount": 300, "description": "Dinner" },
      "timestamp": "2026-06-14T20:01:00Z"
    }
  ]
}
```

**`action` values:** `ExpenseCreated`, `ExpenseUpdated`, `ExpenseDeleted`, `SettlementCreated`, `SettlementDeleted`, `MemberAdded`, `MemberRemoved`, `RoleUpdated`

---

### 7.12 Notifications

#### `GET /api/notifications/unread`
Get all unread notifications for the current user.

**Auth required:** Yes

**Success (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "userId": "uuid",
      "type": "ExpenseAdded",
      "payload": "Sara added a new expense: Dinner — 300 EGP",
      "isRead": false,
      "createdAt": "2026-06-14T20:01:00Z"
    }
  ]
}
```

**`type` values:** `ExpenseAdded`, `ExpenseUpdated`, `ExpenseDeleted`, `SettlementCreated`, `MemberAdded`, `MemberRemoved`

---

#### `POST /api/notifications/mark-read`
Mark multiple notifications as read in one call.

**Auth required:** Yes

**Request:**
```json
{
  "notificationIds": [
    "uuid-1",
    "uuid-2"
  ]
}
```

---

#### `POST /api/notifications/{id}/read`
Mark a single notification as read.

**Auth required:** Yes

---

### 7.13 Files (Upload)

#### `POST /api/files/upload`
Upload an image file (for group logos, etc.).

**Auth required:** Yes

**Content-Type:** `multipart/form-data`

**Request:** Form field `file` containing the image binary

**Accepted types:** Any image (`image/*`)

**Success (200):**
```json
{ "url": "https://res.cloudinary.com/demo/image/upload/v1570979139/sample.jpg" }
```

**Note:** This returns a plain JSON object (not the standard `APIResponse` envelope). Use the `url` directly as `logoUrl` when creating a group.

---

#### `GET /api/files/download?url={cloudinaryUrl}`
Download a file by its Cloudinary URL as an attachment.

**Auth required:** Yes

---

#### `GET /api/files/preview?url={cloudinaryUrl}`
Preview a file inline in the browser/webview.

**Auth required:** Yes

---

## 8. Real-time WebSocket

Connect to receive real-time notifications while the app is in the foreground.

**Endpoint:**
```
ws://<host>/ws/notifications?access_token=<accessToken>
```

**Protocol:** Plain JSON messages over WebSocket

**Authentication:** Pass the JWT as a query parameter (`access_token`). Standard `Authorization` header is not supported in WebSocket handshakes.

**Message format (server → client):**
```json
{
  "type": "ExpenseAdded",
  "payload": "Sara added Dinner — 300 EGP",
  "notificationId": "uuid",
  "timestamp": "2026-06-14T20:01:00Z"
}
```

**Recommended mobile strategy:**
1. Connect on app foreground, disconnect on background
2. On any `type` notification, fetch `GET /api/notifications/unread` to refresh the badge count
3. On disconnect (network loss), reconnect with exponential backoff
4. If the access token has expired, reconnect after refreshing the token

---

## 9. Business Rules Summary

| Rule | Detail |
|------|--------|
| Group membership | Only members can view/add group expenses |
| Admin only | Add/remove members, update roles, create/delete categories |
| Expense splits | Custom splits must sum to total amount |
| Split members | All split userIds must be group members |
| Equal split | Divides amount evenly; applied when `splits` is omitted |
| Currency default | `EGP` if `currency` is not provided |
| Exchange rate | Required (and immutable) for non-EGP expenses |
| Over-settlement blocked | Cannot settle more than what you owe |
| Settlement immutable | Cannot edit a settlement; only delete |
| System categories | Cannot be modified or deleted |
| Personal expenses | Visible only to the creator; not in group balances |
| Token revocation | Logout invalidates all tokens immediately (token version) |

---

## 10. Currency Notes

- Default currency is **EGP** (Egyptian Pound).
- To record an expense in another currency (e.g. USD), you **must** provide an `exchangeRate` value.
- The exchange rate is stored as a snapshot and **cannot be changed after creation**.
- Balances and simplified debts are **grouped by currency** when multi-currency expenses exist.
- The system does not perform currency conversion; it tracks raw amounts per currency.

**Example (USD expense):**
```json
{
  "amount": 100.00,
  "currency": "USD",
  "exchangeRate": 48.50,
  "description": "Hotel booking",
  "expenseDate": "2026-06-14T15:00:00Z"
}
```
