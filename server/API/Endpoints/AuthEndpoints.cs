using Core.DTOs;
using Core.Services;
using System.Security.Claims;

namespace API.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "refreshToken";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/signup", HandleSignup);
        group.MapPost("/login", HandleLogin);
        group.MapPost("/refresh", HandleRefresh);
        group.MapPost("/logout", HandleLogout);
        group.MapGet("/me", HandleMe).RequireAuthorization();
    }

    private static async Task<IResult> HandleSignup(SignupRequest request, IAuthService authService)
    {
        var (response, error) = await authService.SignupAsync(request);

        if (error is not null)
            return Results.BadRequest(new { error });

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleLogin(
        LoginRequest request,
        IAuthService authService,
        HttpContext httpContext)
    {
        var (response, refreshToken, error) = await authService.LoginAsync(request);

        if (error is not null)
            return Results.BadRequest(new { error });

        // Set refresh token as HttpOnly cookie
        SetRefreshTokenCookie(httpContext, refreshToken!);

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleRefresh(
        IAuthService authService,
        HttpContext httpContext)
    {
        var refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrEmpty(refreshToken))
            return Results.Unauthorized();

        var (response, newRefreshToken, error) = await authService.RefreshAsync(refreshToken);

        if (error is not null)
        {
            // Clear the invalid cookie
            ClearRefreshTokenCookie(httpContext);
            return Results.Unauthorized();
        }

        // Set the new rotated refresh token
        SetRefreshTokenCookie(httpContext, newRefreshToken!);

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleLogout(
        IAuthService authService,
        HttpContext httpContext)
    {
        var refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await authService.LogoutAsync(refreshToken);
        }

        ClearRefreshTokenCookie(httpContext);

        return Results.Ok(new { message = "Logged out successfully." });
    }

    private static IResult HandleMe(HttpContext httpContext)
    {
        var user = httpContext.User;

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub");
        var email = user.FindFirstValue(ClaimTypes.Email)
                  ?? user.FindFirstValue("email");
        var role = user.FindFirstValue(ClaimTypes.Role);
        var username = user.FindFirstValue("username");

        if (userId is null)
            return Results.Unauthorized();

        return Results.Ok(new UserDto
        {
            Id = userId,
            Email = email ?? "",
            Role = role ?? "citizen",
            Username = username ?? ""
        });
    }

    private static void SetRefreshTokenCookie(HttpContext httpContext, string token)
    {
        httpContext.Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,           // XSS protection: JS cannot read this cookie
            Secure = true,             // Only sent over HTTPS
            SameSite = SameSiteMode.Strict, // CSRF protection: not sent on cross-origin requests
            Path = "/api/auth",        // Cookie only sent to auth endpoints (minimizes exposure)
            MaxAge = TimeSpan.FromDays(7)
        });
    }

    private static void ClearRefreshTokenCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        });
    }
}
