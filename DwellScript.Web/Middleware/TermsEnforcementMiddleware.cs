using DwellScript.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace DwellScript.Web.Middleware;

/// <summary>
/// Intercepts authenticated requests and redirects users who have not yet accepted
/// the current Terms of Service to the acceptance page before they can proceed.
/// </summary>
public class TermsEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    // Paths that are allowed through even without terms acceptance
    private static readonly string[] AllowedPrefixes =
    [
        "/auth/",
        "/terms/",
        "/home/terms",
        "/home/privacy",
        "/home/error",
        "/favicon",
        "/_framework",
        "/_blazor",
    ];

    public TermsEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        // Only enforce for authenticated users
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip enforcement for allowed paths
        foreach (var prefix in AllowedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
        }

        // Also skip static file extensions
        if (IsStaticFile(path))
        {
            await _next(context);
            return;
        }

        // Check if user has accepted terms
        var user = await userManager.GetUserAsync(context.User);
        if (user?.TermsAcceptedAt == null)
        {
            // Preserve the intended URL as returnUrl (skip for API calls — return 403 instead)
            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "Terms of Service acceptance required." });
                return;
            }

            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/Terms/Accept?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return;
        }

        await _next(context);
    }

    private static bool IsStaticFile(string path)
    {
        var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".woff", ".woff2", ".ttf", ".eot", ".map" };
        return staticExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}
