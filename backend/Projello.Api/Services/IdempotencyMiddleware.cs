using System.Collections.Concurrent;
using System.Security.Claims;

namespace Projello.Api.Services;

// Create and submit requests can carry an Idempotency-Key header. If the same user sends
// the same key again (a retry on a bad site connection, a double tap), the first response
// is replayed instead of creating a duplicate. Keys are kept for 24 hours, in memory.
public class IdempotencyMiddleware
{
    private const string Header = "Idempotency-Key";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private sealed record Saved(int Status, string? ContentType, byte[] Body, DateTime Expires);

    private static readonly ConcurrentDictionary<string, Saved> Responses = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var key = context.Request.Headers[Header].ToString();
        if (context.Request.Method != HttpMethods.Post || string.IsNullOrWhiteSpace(key) || key.Length > 100)
        {
            await _next(context);
            return;
        }

        // Scope keys to the user and the endpoint so they can't collide or be replayed by someone else
        var user = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var cacheKey = $"{user}|{context.Request.Path}|{key}";

        var gate = Locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            if (Responses.TryGetValue(cacheKey, out var saved) && saved.Expires > DateTime.UtcNow)
            {
                context.Response.StatusCode = saved.Status;
                if (saved.ContentType != null) context.Response.ContentType = saved.ContentType;
                context.Response.Headers["Idempotent-Replay"] = "true";
                await context.Response.Body.WriteAsync(saved.Body);
                return;
            }

            var original = context.Response.Body;
            using var buffer = new MemoryStream();
            context.Response.Body = buffer;
            try
            {
                await _next(context);
            }
            finally
            {
                context.Response.Body = original;
            }

            var body = buffer.ToArray();
            // Only remember successes: a failed attempt should be retryable with the same key
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                Responses[cacheKey] = new Saved(context.Response.StatusCode, context.Response.ContentType, body, DateTime.UtcNow.Add(Lifetime));
            }
            await original.WriteAsync(body);
        }
        finally
        {
            gate.Release();
            PruneExpired();
        }
    }

    private static void PruneExpired()
    {
        if (Responses.Count < 1000) return;
        var now = DateTime.UtcNow;
        foreach (var (k, v) in Responses)
        {
            if (v.Expires <= now)
            {
                Responses.TryRemove(k, out _);
                Locks.TryRemove(k, out _);
            }
        }
    }
}
