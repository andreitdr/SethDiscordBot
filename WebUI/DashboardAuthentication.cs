using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using WebUI.Services;

namespace WebUI;

/// <summary>
/// Password sign-in for the dashboard. With <c>Dashboard:Password</c> set, every page, the Blazor circuit and the plugin
/// pages need the sign-in cookie. Without it, the dashboard only answers requests from this machine.
/// </summary>
public static class DashboardAuthentication
{
    public const string LoginPath = "/login";
    public const string LogoutPath = "/logout";

    private const string CookieName = "SethDiscordBot.Auth";
    private const string KeysFolder = "./Data/Keys";
    private const int LoginAttemptsPerMinute = 5;

    public static void AddDashboardAuthentication(this WebApplicationBuilder builder)
    {
        var access = new DashboardAccess(builder.Configuration);
        builder.Services.AddSingleton(access);

        // The cookie and antiforgery tokens are encrypted with these keys; keeping them in Data means a restart or
        // a new container doesn't sign everyone out.
        Directory.CreateDirectory(KeysFolder);
        builder.Services.AddDataProtection()
            .SetApplicationName("SethDiscordBot")
            .PersistKeysToFileSystem(new DirectoryInfo(KeysFolder));

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.LoginPath = LoginPath;
                options.LogoutPath = LogoutPath;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.Events.OnValidatePrincipal = context =>
                {
                    string? stamp = context.Principal?.FindFirst(DashboardAccess.PasswordStampClaimType)?.Value;
                    if (stamp != access.PasswordStamp)
                    {
                        context.RejectPrincipal();
                        return context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }

                    return Task.CompletedTask;
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            if (access.IsPasswordRequired)
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            }
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                HttpMethods.IsPost(context.Request.Method) && context.Request.Path.Equals(LoginPath, StringComparison.OrdinalIgnoreCase)
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = LoginAttemptsPerMinute, Window = TimeSpan.FromMinutes(1) })
                    : RateLimitPartition.GetNoLimiter(string.Empty));
        });
    }

    public static void UseDashboardAuthentication(this WebApplication app)
    {
        var access = app.Services.GetRequiredService<DashboardAccess>();

        if (!access.IsPasswordRequired)
        {
            if (access.AllowRemoteWithoutPassword)
            {
                app.Logger.LogWarning("No dashboard password is set and remote access is allowed: anyone who can reach this port controls the bot.");
            }
            else
            {
                app.Logger.LogWarning("No dashboard password is set, so the dashboard only accepts connections from this machine. Set Dashboard__Password to use it remotely.");
                app.Use(RejectRemoteRequests);
            }
        }

        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
    }

    public static void MapDashboardLogout(this WebApplication app)
    {
        // Posted from the interactive header, which has no antiforgery token. The sign-in cookie is SameSite=Strict,
        // so another site can't make a signed-in browser send this anyway.
        app.MapPost(LogoutPath, async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.LocalRedirect("~" + LoginPath);
            })
            .AllowAnonymous()
            .DisableAntiforgery();
    }

    /// <summary>
    /// Whether a return URL stays inside the app: "/x" does, "//host" and "/\host" would leave it.
    /// </summary>
    public static bool IsLocalUrl(string? url) =>
        !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));

    private static async Task RejectRemoteRequests(HttpContext context, Func<Task> next)
    {
        IPAddress? remote = context.Connection.RemoteIpAddress;
        if (remote is not null && remote.IsIPv4MappedToIPv6)
        {
            remote = remote.MapToIPv4();
        }

        if (remote is null || IPAddress.IsLoopback(remote))
        {
            await next();
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync(
            "The Seth Discord Bot dashboard has no password, so it only accepts connections from the machine it runs on.\n" +
            "Set the Dashboard__Password environment variable and restart it to sign in from other devices.");
    }
}
