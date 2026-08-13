using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using CarPlates.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarPlates.API.Configuration;

public static class JwtConfiguration
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // IMPORTANT: this must read from the SAME configuration source that
        // JwtService uses to sign tokens (the JWT__Key / JWT__Issuer / JWT__Audience
        // environment variables, which ASP.NET Core binds to the "JWT" config
        // section). Previously this read a "JwtSettings" section from
        // appsettings.json that only ever contained un-substituted "${...}"
        // placeholder text, so every token signed by JwtService failed
        // validation here with a completely different key/issuer/audience -
        // every authenticated call would 401 right after a successful login.
        var jwtSettings = configuration.GetSection("JWT");
        var secret = jwtSettings["Key"]
            ?? throw new InvalidOperationException("JWT__Key is missing.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                // Each access token is bound to a session (sessionId claim). The session
                // stays valid only while it has a live, non-revoked refresh token. Logging
                // in on another device revokes every other session for the user, so their
                // existing access token stops working on the next request.
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? principal?.FindFirstValue("sub");
                    var sessionId = principal?.FindFirstValue("sessionId");

                    if (string.IsNullOrWhiteSpace(userId) ||
                        string.IsNullOrWhiteSpace(sessionId) ||
                        !Guid.TryParse(sessionId, out var sessionGuid))
                    {
                        context.Fail("Token is missing session information.");
                        return;
                    }

                    var dbContext = context.HttpContext.RequestServices
                        .GetRequiredService<ApplicationDbContext>();

                    try
                    {
                        var sessionIsValid = await dbContext.RefreshTokens
                            .AnyAsync(t =>
                                t.UserId == userId &&
                                t.SessionId == sessionGuid &&
                                !t.Revoked &&
                                t.ExpiresAt > DateTime.UtcNow);

                        if (!sessionIsValid)
                        {
                            context.Fail("Session has been revoked.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Fail open: a transient DB outage must not log every user
                        // out. The token is still cryptographically valid, and any
                        // endpoint that actually needs the database will surface its
                        // own error without destroying the client's session.
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtSessionValidation");
                        logger.LogWarning(ex,
                            "Session validation skipped due to a database error for user {UserId}.",
                            userId);
                    }
                }
            };
        });


        return services;
    }
}
