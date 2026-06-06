using System.Security.Claims;
using Core.DTOs;
using Core.Services;
using Microsoft.AspNetCore.Authorization;

namespace API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/signup", HandleSignup);
        group.MapPost("/login", HandleLogin);
        group.MapGet("/me", HandleMe).RequireAuthorization();
    }

    private static async Task<IResult> HandleSignup(SignupRequest request, IAuthService authService)
    {
        var (response, error) = await authService.SignupAsync(request);
        return error is not null ? Results.BadRequest(new { error }) : Results.Ok(response);
    }

    private static async Task<IResult> HandleLogin(LoginRequest request, IAuthService authService)
    {
        var (response, error) = await authService.LoginAsync(request);
        return error is not null ? Results.BadRequest(new { error }) : Results.Ok(response);
    }

    private static IResult HandleMe(HttpContext httpContext)
    {
        var user = httpContext.User;

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue(ClaimTypes.Name)
                  ?? user.FindFirstValue("sub");
        var email = user.FindFirstValue(ClaimTypes.Email)
                  ?? user.FindFirstValue("email");
        var username = user.FindFirstValue(ClaimTypes.Name)
                  ?? user.FindFirstValue("username");
        var role = user.FindFirstValue(ClaimTypes.Role) ?? "citizen";

        if (string.IsNullOrWhiteSpace(userId))
            return Results.Unauthorized();

        return Results.Ok(new UserDto
        {
            Id = userId,
            Email = email ?? string.Empty,
            Username = username ?? string.Empty,
            Role = role,
            Points = 0
        });
    }
}
