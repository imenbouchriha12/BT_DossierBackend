using System.Collections.Concurrent;
using System.Net;

namespace Backend.API.Middlewares
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _clients = new();

        public RateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            var isAuthEndpoint = endpoint.StartsWith("/api/auth/login") || endpoint.StartsWith("/api/auth/register");
            var isSensitiveEndpoint = endpoint.StartsWith("/api/clients") ||
                                       endpoint.StartsWith("/api/documents") ||
                                       endpoint.StartsWith("/api/reports") ||
                                       endpoint.StartsWith("/api/search") ||
                                       endpoint.StartsWith("/api/advanced-search") ||
                                       endpoint.StartsWith("/api/export");

            if (!isAuthEndpoint && !isSensitiveEndpoint)
            {
                await _next(context);
                return;
            }

            var limit = isAuthEndpoint ? 10 : 60;
            var windowSeconds = isAuthEndpoint ? 60 : 60;

            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{clientIp}:{endpoint}";
            var now = DateTime.UtcNow;

            var entry = _clients.AddOrUpdate(key,
                _ => (1, now),
                (_, existing) =>
                {
                    if (now - existing.WindowStart > TimeSpan.FromSeconds(windowSeconds))
                        return (1, now);
                    return (existing.Count + 1, existing.WindowStart);
                });

            var remaining = limit - entry.Count;
            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, remaining).ToString();

            if (entry.Count > limit)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["RetryAfter"] = windowSeconds.ToString();
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
