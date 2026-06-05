using System.Security.Cryptography;

namespace API.Middleware;

/// <summary>
/// Double-submit cookie CSRF protection middleware.
/// 
/// How it works:
/// 1. On every response, sets a readable (non-HttpOnly) cookie "XSRF-TOKEN" with a random value.
/// 2. On state-changing requests (POST/PUT/DELETE/PATCH), the client must read that cookie
///    and echo its value in the "X-XSRF-TOKEN" request header.
/// 3. Since a cross-origin attacker cannot read cookies from our domain (same-origin policy),
///    they cannot forge the header — blocking CSRF attacks.
///
/// Endpoints excluded from CSRF validation:
/// - /api/auth/signup and /api/auth/login (open endpoints with no cookies to abuse)
/// - Any GET/HEAD/OPTIONS request (safe methods)
/// </summary>
public class CsrfMiddleware
{
    private readonly RequestDelegate _next;
    private const string CsrfCookieName = "XSRF-TOKEN";
    private const string CsrfHeaderName = "X-XSRF-TOKEN";

    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/signup",
        "/api/auth/login"
    };

    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS"
    };

    public CsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";

        // Only validate on state-changing methods
        if (!SafeMethods.Contains(method))
        {
            // Skip excluded paths and team endpoints (which are migrating to auth)
            if (!ExcludedPaths.Contains(path) && !path.StartsWith("/api/teams", StringComparison.OrdinalIgnoreCase))
            {
                var cookieValue = context.Request.Cookies[CsrfCookieName];
                var headerValue = context.Request.Headers[CsrfHeaderName].FirstOrDefault();

                if (string.IsNullOrEmpty(cookieValue) ||
                    string.IsNullOrEmpty(headerValue) ||
                    !string.Equals(cookieValue, headerValue, StringComparison.Ordinal))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "CSRF validation failed." });
                    return;
                }
            }
        }

        await _next(context);

        // Set/refresh the CSRF cookie on every response (non-HttpOnly so JS can read it)
        if (!context.Response.HasStarted)
        {
            var token = GenerateCsrfToken();
            context.Response.Cookies.Append(CsrfCookieName, token, new CookieOptions
            {
                HttpOnly = false,          // JS must be able to read this
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                MaxAge = TimeSpan.FromHours(2)
            });
        }
    }

    private static string GenerateCsrfToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
