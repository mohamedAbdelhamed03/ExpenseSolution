using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Notifications
{
    public interface IWebSocketConnectionManager
    {
        void AddSocket(string userId, WebSocket socket);
        Task RemoveSocketAsync(string userId, WebSocket socket);
        IEnumerable<WebSocket> GetSockets(string userId);
        IEnumerable<WebSocket> GetAllSockets();
    }

    public class WebSocketConnectionManager : IWebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, List<WebSocket>> _sockets = new();

        public void AddSocket(string userId, WebSocket socket)
        {
            _sockets.AddOrUpdate(userId,
                key => new List<WebSocket> { socket },
                (key, list) =>
                {
                    lock (list)
                    {
                        list.Add(socket);
                    }
                    return list;
                });
        }

        public async Task RemoveSocketAsync(string userId, WebSocket socket)
        {
            if (_sockets.TryGetValue(userId, out var list))
            {
                lock (list)
                {
                    list.Remove(socket);
                }

                if (list.Count == 0)
                {
                    _sockets.TryRemove(userId, out _);
                }

                if (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted)
                {
                    try
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by manager", CancellationToken.None);
                    }
                    catch
                    {
                        // Ignore errors during closure
                    }
                }
            }
        }

        public IEnumerable<WebSocket> GetSockets(string userId)
        {
            if (_sockets.TryGetValue(userId, out var list))
            {
                lock (list)
                {
                    return list.ToList(); // Return copy
                }
            }
            return Enumerable.Empty<WebSocket>();
        }

        public IEnumerable<WebSocket> GetAllSockets()
        {
            return _sockets.Values.SelectMany(x => x).ToList();
        }
    }
}
