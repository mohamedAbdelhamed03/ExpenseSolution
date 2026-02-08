# Real‑Time Notifications (Native WebSockets) – Plan

## Overview
- Native WebSocket endpoint: `/ws/notifications`
- JWT via query string (`?access_token=`)
- Best‑effort delivery; never blocks transactions
- WebSocket logic in Infrastructure only

## Core Components
1. WebSocket middleware endpoint
2. `IWebSocketConnectionManager` + implementation
3. `IRealtimeNotifier` + `NativeWebSocketNotifier`
4. Event handlers (persist notification + call notifier)
5. Tests
6. Minimal docs

## WebSocket Middleware
- Accept WebSocket requests at `/ws/notifications`
- Validate JWT from query string
- Resolve `userId` from claims
- Register connection
- Handle receive loop and disconnect cleanup

## Connection Manager
- Thread‑safe map: `userId -> List<WebSocket>`
- Add/remove sockets
- Cleanup dead sockets

## Realtime Notifier
- `NotifyUserAsync(userId, payload)`
- `NotifyUsersAsync(userIds, payload)`
- Serialize to JSON and send
- Remove dead sockets
- Never throw; best‑effort only

## Events & Notification Flow
- Business services emit internal events
- Event handlers:
  - Persist notification in DB
  - Invoke notifier (fire‑and‑forget)
  - Do not block main transaction

## Message Contract
```json
{
  "type": "ExpenseCreated",
  "groupId": "guid",
  "actor": "userId",
  "payload": { "expenseId": "guid" },
  "timestamp": "2026-02-08T12:00:00Z"
}
```

## Trigger Matrix
- Expense created/updated/deleted → notify all group members except actor
- Settlement created/deleted → notify all group members except actor

## Tests
### Unit
- Notifier sends to multiple sockets per user
- Dead sockets are removed on failure
- Event handler calls notifier with correct recipients

### Integration
- WebSocket client connects with JWT
- Expense created → notification received
- Actor excluded from recipients

## Constraints
- No SignalR
- No brokers/queues
- No retries
- No auto‑settlement
- No WebSocket logic in Domain/Application
