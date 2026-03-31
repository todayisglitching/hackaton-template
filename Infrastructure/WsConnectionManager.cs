using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace testASP.Infrastructure;

public sealed class WsConnectionManager
{
    private readonly ConcurrentDictionary<int, List<WebSocket>> _connections = new();

    public List<WebSocket> GetOrCreate(int userId)
    {
        return _connections.GetOrAdd(userId, _ => new List<WebSocket>());
    }

    public void Add(int userId, WebSocket socket)
    {
        var list = GetOrCreate(userId);
        lock (list)
        {
            list.Add(socket);
        }
    }

    public void Remove(int userId, WebSocket socket)
    {
        if (!_connections.TryGetValue(userId, out var list)) return;
        lock (list)
        {
            list.Remove(socket);
        }
    }
}
