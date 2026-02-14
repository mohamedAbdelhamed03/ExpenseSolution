using System.Net.WebSockets;
using System.Security.Claims;
using Expense.Infrastructure.Notifications;


namespace Expense.API.Middlewares
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly IWebSocketConnectionManager _connectionManager;

        public WebSocketMiddleware(RequestDelegate next, ILogger<WebSocketMiddleware> logger, IWebSocketConnectionManager connectionManager)
        {
            _next = next;
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/ws/notifications")
            {
                _logger.LogDebug("WebSocket endpoint hit. TraceId: {TraceId}", context.TraceIdentifier);

                if (context.WebSockets.IsWebSocketRequest)
                {
                    // Auth should have been handled by JwtBearer middleware (OnMessageReceived)
                    if (context.User.Identity?.IsAuthenticated != true)
                    {
                        _logger.LogWarning("Unauthorized WebSocket request rejected. TraceId: {TraceId}", context.TraceIdentifier);
                        context.Response.StatusCode = 401;
                        return;
                    }

                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        _logger.LogWarning("WebSocket request missing user identifier. TraceId: {TraceId}", context.TraceIdentifier);
                        context.Response.StatusCode = 401;
                        return;
                    }

                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    _connectionManager.AddSocket(userId, webSocket);
                    _logger.LogInformation("WebSocket connected. UserId: {UserId}, TraceId: {TraceId}", userId, context.TraceIdentifier);

                    await ReceiveLoop(webSocket, userId);
                }
                else
                {
                    _logger.LogWarning("Invalid WebSocket handshake request. TraceId: {TraceId}", context.TraceIdentifier);
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task ReceiveLoop(WebSocket socket, string userId)
        {
            var buffer = new byte[1024 * 4];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket disconnected. UserId: {UserId}", userId);
                        await _connectionManager.RemoveSocketAsync(userId, socket);
                        break;
                    }
                    // We don't expect client messages in this system, but we keep the loop alive
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket receive loop failed. UserId: {UserId}", userId);
                await _connectionManager.RemoveSocketAsync(userId, socket);
            }
        }
    }
}
