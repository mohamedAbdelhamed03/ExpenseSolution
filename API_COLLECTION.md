# API Collection Specification

## Common Headers
- `Authorization: Bearer <JWT>` (required for protected endpoints)
- `Content-Type: application/json`
- `Accept-Language: en` (optional)

## Common Error Response Format
```json
{
  "success": false,
  "message": null,
  "data": null,
  "errors": ["ValidationErrorCodeOrMessage"],
  "statusCode": 400,
  "timestamp": "2026-02-08T12:00:00Z",
  "traceId": "00-abc123..."
}
```

## Auth

### POST /api/auth/register
- Description: Register a new user
- Auth required: No
- Request body:
```json
{
  "email": "user@example.com",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "firstName": "Omar",
  "lastName": "Hassan",
  "phoneNumber": "+201234567890"
}
```
- Success response:
```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "success": true,
    "message": "Registration successful",
    "userId": "b5f5d9b8-0c7d-4e1d-9f28-5bfa2f5ed0a1"
  },
  "errors": [],
  "statusCode": 200
}
```
- Error responses: 400 (validation), 409 (email exists)

### POST /api/auth/login
- Description: Login and obtain tokens
- Auth required: No
- Request body:
```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```
- Success response:
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "success": true,
    "message": "Login successful",
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "b1c2d3e4...",
    "expiresAt": "2026-02-08T13:00:00Z",
    "userId": "b5f5d9b8-0c7d-4e1d-9f28-5bfa2f5ed0a1",
    "email": "user@example.com",
    "roles": ["User"]
  },
  "errors": [],
  "statusCode": 200
}
```
- Error responses: 401 (invalid credentials)

### POST /api/auth/social-login
- Description: Login or register using a social provider (Google/Facebook)
- Auth required: No
- Request body:
```json
{
  "token": "google_or_facebook_token",
  "provider": "Google" 
}
```
- Success response: Same as Login
- Error responses: 400 (Invalid token or provider)

## Home Feed

### GET /api/home
- Description: Get unified feed of expenses, settlements, and personal expenses.
- Auth required: Yes
- Query params: `page` (default 1), `pageSize` (default 10)
- Response includes `HomeFeedItemDto` with `Direction` (In/Out/Neutral).

## Groups

### POST /api/groups
- Description: Create a group
- Auth required: Yes
- Request body:
```json
{
  "name": "Trip to Paris",
  "logoUrl": "https://example.com/logo.png"
}
```

### GET /api/groups
- Description: List groups for current user
- Auth required: Yes

### POST /api/groups/join/{inviteCode}
- Description: Join a group by invite code
- Auth required: Yes

### GET /api/groups/{groupId}
- Description: Get group details
- Auth required: Yes

### GET /api/groups/{groupId}/members
- Description: List group members
- Auth required: Yes

### PUT /api/groups/{groupId}/members/{userId}/role
- Description: Update member role (full update)
- Auth required: Yes

### PATCH /api/groups/{groupId}/members/{userId}/role
- Description: Update member role (partial)
- Auth required: Yes

### DELETE /api/groups/{groupId}/members/{userId}
- Description: Remove member
- Auth required: Yes

## Expenses

### POST /api/groups/{groupId}/expenses
- Description: Create expense (equal split if splits omitted)
- Auth required: Yes
- Request body:
```json
{
  "amount": 150.00,
  "currency": "EGP",
  "exchangeRate": null,
  "description": "Dinner",
  "categoryId": "7f9f62d1-7c2a-4d8d-9c6f-0b2c4a8e9c11",
  "expenseDate": "2026-02-08T12:00:00Z",
  "splits": [
    {"userId": "u1", "amount": 75.00},
    {"userId": "u2", "amount": 75.00}
  ]
}
```

### GET /api/groups/{groupId}/expenses
- Description: List group expenses
- Auth required: Yes

### GET /api/expenses/{expenseId}
- Description: Get single expense
- Auth required: Yes

### PUT /api/expenses/{expenseId}
- Description: Update expense (full update)
- Auth required: Yes

### PATCH /api/expenses/{expenseId}
- Description: Update expense (partial)
- Auth required: Yes

### DELETE /api/expenses/{expenseId}
- Description: Delete expense
- Auth required: Yes

## Personal Expenses

### POST /api/personal-expenses
- Description: Create a personal expense
- Auth required: Yes
- Request body:
```json
{
  "amount": 50.00,
  "currency": "USD",
  "date": "2026-02-10T12:00:00Z",
  "description": "Coffee",
  "categoryId": "7f9f62d1-7c2a-4d8d-9c6f-0b2c4a8e9c11"
}
```

### GET /api/personal-expenses
- Description: Get list of personal expenses
- Auth required: Yes

### GET /api/personal-expenses/{id}
- Description: Get single personal expense
- Auth required: Yes

### PUT /api/personal-expenses/{id}
- Description: Update personal expense (full update)
- Auth required: Yes

### PATCH /api/personal-expenses/{id}
- Description: Update personal expense (partial)
- Auth required: Yes

### DELETE /api/personal-expenses/{id}
- Description: Delete personal expense
- Auth required: Yes

## Files

### POST /api/files/upload
- Description: Upload an image file (e.g. for group or category logo). Returns the public URL.
- Auth required: Yes
- Content-Type: multipart/form-data
- Request body:
  - `file`: (File, binary)
- Success response:
```json
{
  "url": "https://res.cloudinary.com/demo/image/upload/v1570979139/sample.jpg"
}
```
- Error responses: 400 (empty file or invalid type)

### GET /api/files/download
- Description: Download a file by URL.
- Auth required: Yes
- Query params: `url` (Cloudinary URL)
- Response: File attachment

### GET /api/files/preview
- Description: Preview a file by URL.
- Auth required: Yes
- Query params: `url` (Cloudinary URL)
- Response: Inline file content

## Balances

### GET /api/groups/{groupId}/balances
- Description: Get balances for group
- Auth required: Yes

## Settlements

### POST /api/groups/{groupId}/settlements
- Description: Create settlement (over‑settlement blocked)
- Auth required: Yes

### GET /api/groups/{groupId}/settlements
- Description: List settlements for group
- Auth required: Yes

## Debts

### GET /api/groups/{groupId}/debts/simplified
- Description: Get simplified debt graph (minimized transactions) to settle up efficiently.
- Auth required: Yes

## Insights

### GET /api/groups/{groupId}/insights
- Description: Get expense insights for a group (totals, category breakdown, percentages).
- Auth required: Yes
- Query params: `period=month|year|all`, `date=YYYY-MM|YYYY`, `scope=group|me`

### GET /api/insights/home
- Description: Get aggregated insights for the current user across all groups and personal expenses.
- Auth required: Yes
- Query params: `period=month|year|all`, `date=YYYY-MM|YYYY`

## Categories

### POST /api/groups/{groupId}/categories
- Description: Create category (admin only)
- Auth required: Yes
- Request body:
```json
{
  "name": "Transport",
  "description": "Bus, Train, Flight",
  "logoUrl": "https://example.com/transport.png"
}
```

### GET /api/groups/{groupId}/categories
- Description: List categories for a group
- Auth required: Yes

### PUT /api/categories/{categoryId}
- Description: Update category (full update, admin only)
- Auth required: Yes
- Request body:
```json
{
  "name": "Transport Updated",
  "description": "Bus, Train, Flight, Taxi",
  "logoUrl": "https://example.com/transport_new.png"
}
```

### PATCH /api/categories/{categoryId}
- Description: Update category (partial, admin only)
- Auth required: Yes
- Request body:
```json
{
  "logoUrl": "https://example.com/transport_new.png"
}
```

### DELETE /api/categories/{categoryId}
- Description: Delete category (admin only)
- Auth required: Yes

## Activity Logs

### GET /api/groups/{groupId}/activities
- Description: List group activity logs (paged)
- Auth required: Yes
- Query params: `page` (default 1), `pageSize` (default 20)

## Notifications

### GET /api/notifications/unread
- Description: Get unread notifications for the current user
- Auth required: Yes

### POST /api/notifications/mark-read
- Description: Mark multiple notifications as read
- Auth required: Yes

### POST /api/notifications/{id}/read
- Description: Mark a single notification as read
- Auth required: Yes

## Realtime

### WebSocket /ws/notifications
- Description: Realtime notification stream
- Query params: `access_token=<JWT>`
- Protocol: JSON messages
