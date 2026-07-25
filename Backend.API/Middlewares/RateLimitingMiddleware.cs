using System.Collections.Concurrent;
using System.Net;

namespace Backend.API.Middlewares
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _maxRequests;
        private readonly TimeSpan _window;
        private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _clients = new();

        public RateLimitingMiddleware(RequestDelegate next, int maxRequests = 10, int windowSeconds = 60)
        {
            _next = next;
            _maxRequests = maxRequests;
            _window = TimeSpan.FromSeconds(windowSeconds);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            if (!endpoint.StartsWith("/api/auth/login") && !endpoint.StartsWith("/api/auth/register"))
            {
                await _next(context);
                return;
            }

            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{clientIp}:{endpoint}";
            var now = DateTime.UtcNow;

            var entry = _clients.AddOrUpdate(key,
                _ => (1, now),
                (_, existing) =>
                {
                    if (now - existing.WindowStart > _window)
                        return (1, now);
                    return (existing.Count + 1, existing.WindowStart);
                });

            var remaining = _maxRequests - entry.Count;
            context.Response.Headers["X-RateLimit-Limit"] = _maxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, remaining).ToString();

            if (entry.Count > _maxRequests)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["RetryAfter"] = _window.TotalSeconds.ToString();
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Trop de requêtes. Réessayez dans quelques instants."
                });
                return;
            }

            await _next(context);
        }
    }
}
