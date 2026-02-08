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

## Groups

### POST /api/groups
- Description: Create a group
- Auth required: Yes
- Request body:
```json
{
  "name": "Weekend Trip"
}
```
- Success response:
```json
{
  "success": true,
  "message": "Group created successfully",
  "data": {
    "id": "c1a5b9de-3c6d-4f5b-8b1d-9a4d2fbb2a10",
    "name": "Weekend Trip",
    "inviteCode": "a1b2c3d4",
    "members": [
      {"userId": "b5f5d9b8-0c7d-4e1d-9f28-5bfa2f5ed0a1", "role": "Admin"}
    ]
  },
  "errors": [],
  "statusCode": 200
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
- Description: Update member role
- Auth required: Yes
- Request body:
```json
{ "role": "Admin" }
```

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
- Description: Update expense
- Auth required: Yes

### DELETE /api/expenses/{expenseId}
- Description: Delete expense
- Auth required: Yes

## Balances

### GET /api/groups/{groupId}/balances
- Description: Get balances for group
- Auth required: Yes

## Settlements

### POST /api/groups/{groupId}/settlements
- Description: Create settlement (over‑settlement blocked)
- Auth required: Yes
- Request body:
```json
{
  "payeeUserId": "u1",
  "amount": 80.00,
  "currency": "EGP",
  "exchangeRate": null,
  "settlementDate": "2026-02-08T12:30:00Z"
}
```

### GET /api/groups/{groupId}/settlements
- Description: List settlements for group
- Auth required: Yes

## Debts

### GET /api/groups/{groupId}/debts/simplified
- Description: Get simplified debt graph (minimized transactions) to settle up efficiently.
- Auth required: Yes
- Success response:
```json
{
  "success": true,
  "message": null,
  "data": [
    {
      "currency": "USD",
      "transfers": [
        {
          "fromUserId": "u1",
          "toUserId": "u2",
          "amount": 50.00,
          "currency": "USD"
        }
      ]
    }
  ],
  "errors": [],
  "statusCode": 200
}
```

## Categories

### POST /api/groups/{groupId}/categories
- Description: Create category (admin only)
- Auth required: Yes

### GET /api/groups/{groupId}/categories
- Description: List categories for a group
- Auth required: Yes

### PUT /api/categories/{categoryId}
- Description: Update category (admin only)
- Auth required: Yes

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
- Success response:
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "type": "Expense_Created",
      "payload": "json_string",
      "isRead": false,
      "createdAt": "2026-02-08T12:00:00Z"
    }
  ]
}
```

### POST /api/notifications/mark-read
- Description: Mark multiple notifications as read
- Auth required: Yes
- Request body:
```json
{
  "notificationIds": ["uuid1", "uuid2"]
}
```

### POST /api/notifications/{id}/read
- Description: Mark a single notification as read
- Auth required: Yes

## Realtime
### WebSocket /ws
- Description: Realtime notification stream
- Query params: `token=<access_token>`
- Protocol: JSON messages

