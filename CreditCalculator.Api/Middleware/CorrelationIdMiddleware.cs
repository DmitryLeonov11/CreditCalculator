using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace CreditCalculator.Api.Middleware;

public sealed partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestedId = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = requestedId is not null && IsValidCorrelationId().IsMatch(requestedId)
            ? requestedId
            : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>();
            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                logger.LogInformation("Incoming HTTP request {Method} {Path}", context.Request.Method, context.Request.Path);
                await _next(context);
            }
        }
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex IsValidCorrelationId();
}