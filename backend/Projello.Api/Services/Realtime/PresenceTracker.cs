using System.Collections.Concurrent;

namespace Projello.Api.Services.Realtime;

// Who is signed in right now, based on open team-notification connections.
// One user can have several connections (for example two devices), so we count them.
// In memory: fine for a single API instance like the Render service.
public class PresenceTracker
{
    private readonly ConcurrentDictionary<string, int> _connections = new();

    public void Connected(string userId) =>
        _connections.AddOrUpdate(userId, 1, (_, count) => count + 1);

    public void Disconnected(string userId)
    {
        while (_connections.TryGetValue(userId, out var count))
        {
            var updated = _connections.TryUpdate(userId, count - 1, count);
            if (!updated) continue;
            if (count - 1 <= 0) _connections.TryRemove(new KeyValuePair<string, int>(userId, 0));
            return;
        }
    }

    public bool IsOnline(string userId) =>
        _connections.TryGetValue(userId, out var count) && count > 0;
}
