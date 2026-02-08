using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Notifications;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Notifications;
using Microsoft.Extensions.Logging;

namespace Expense.Infrastructure.Notifications
{
    public class NativeWebSocketNotifier : IRealtimeNotifier
    {
        private readonly IWebSocketConnectionManager _connectionManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NativeWebSocketNotifier> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public NativeWebSocketNotifier(
            IWebSocketConnectionManager connectionManager, 
            IUnitOfWork unitOfWork,
            ILogger<NativeWebSocketNotifier> logger)
        {
            _connectionManager = connectionManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task NotifyUserAsync(string userId, NotificationMessage message, CancellationToken cancellationToken = default)
        {
            // 1. Persist Notification
            try 
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = message.Type,
                    GroupId = message.GroupId,
                    Message = JsonSerializer.Serialize(message.Payload, _jsonOptions),
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.Repository<Notification>().Add(notification);
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist notification for user {UserId}", userId);
                // Continue to send realtime notification even if persistence fails? 
                // Probably yes, or maybe not. Let's continue.
            }

            // 2. Send Realtime
            var sockets = _connectionManager.GetSockets(userId);
            if (!sockets.Any()) return;

            var payload = JsonSerializer.Serialize(message, _jsonOptions);
            var buffer = Encoding.UTF8.GetBytes(payload);
            var segment = new ArraySegment<byte>(buffer);

            var tasks = new List<Task>();

            foreach (var socket in sockets)
            {
                if (socket.State == WebSocketState.Open)
                {
                    tasks.Add(SendAsync(socket, segment, userId, cancellationToken));
                }
                else
                {
                    tasks.Add(_connectionManager.RemoveSocketAsync(userId, socket));
                }
            }

            await Task.WhenAll(tasks);
        }

        public async Task NotifyUsersAsync(IEnumerable<string> userIds, NotificationMessage message, CancellationToken cancellationToken = default)
        {
            // Optimize persistence: Batch insert
            // But _unitOfWork is scoped, so we can just loop add and then save once.
            
            try
            {
                foreach (var userId in userIds)
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Type = message.Type,
                        GroupId = message.GroupId,
                        Message = JsonSerializer.Serialize(message.Payload, _jsonOptions),
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _unitOfWork.Repository<Notification>().Add(notification);
                }
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist notifications for users");
            }

            // Send Realtime (Parallel)
            var tasks = userIds.Select(uid => NotifyUserRealtimeOnlyAsync(uid, message, cancellationToken));
            await Task.WhenAll(tasks);
        }

        private async Task NotifyUserRealtimeOnlyAsync(string userId, NotificationMessage message, CancellationToken cancellationToken)
        {
             var sockets = _connectionManager.GetSockets(userId);
            if (!sockets.Any()) return;

            var payload = JsonSerializer.Serialize(message, _jsonOptions);
            var buffer = Encoding.UTF8.GetBytes(payload);
            var segment = new ArraySegment<byte>(buffer);

            var tasks = new List<Task>();

            foreach (var socket in sockets)
            {
                if (socket.State == WebSocketState.Open)
                {
                    tasks.Add(SendAsync(socket, segment, userId, cancellationToken));
                }
                else
                {
                    tasks.Add(_connectionManager.RemoveSocketAsync(userId, socket));
                }
            }

            await Task.WhenAll(tasks);
        }

        private async Task SendAsync(WebSocket socket, ArraySegment<byte> buffer, string userId, CancellationToken cancellationToken)
        {
            try
            {
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send message to user {UserId}", userId);
                await _connectionManager.RemoveSocketAsync(userId, socket);
            }
        }
    }
}
