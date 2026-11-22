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
        // JwtService uses to sign tokens (the CARPLATES_JWT_KEY / CARPLATES_JWT_ISSUER / CARPLATES_JWT_AUDIENCE
        // environment variables, which ASP.NET Core binds to the "JWT" config
        // section). Previously this read a "JwtSettings" section from
        // appsettings.json that only ever contained un-substituted "${...}"
        // placeholder text, so every token signed by JwtService failed
        // validation here with a completely different key/issuer/audience -
        // every authenticated call would 401 right after a successful login.
        var jwtSettings = configuration.GetSection("JWT");
        var secret = jwtSettings["Key"];
        if (string.IsNullOrWhiteSpace(secret) || secret.StartsWith("${", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "JWT signing key is missing. Set the CARPLATES_JWT_KEY environment variable (see SECURITY-NOTES.md).");
        var jwtIssuer = jwtSettings["Issuer"];
        var jwtAudience = jwtSettings["Audience"];
        if (string.IsNullOrWhiteSpace(jwtIssuer) || jwtIssuer.StartsWith("${", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(jwtAudience) || jwtAudience.StartsWith("${", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "JWT issuer/audience are missing. Set the CARPLATES_JWT_ISSUER and CARPLATES_JWT_AUDIENCE environment variables (see SECURITY-NOTES.md).");

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
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
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

                    // Signature/issuer/audience/lifetime have already passed here.
                    // Publish that principal before tenant-scoped DbContext construction.
                    // Session validation must use the claim tenant, never an anonymous header.
                    var previousPrincipal = context.HttpContext.User;
                    context.HttpContext.User = principal!;
                    try
                    {
                        var dbContext = context.HttpContext.RequestServices
                            .GetRequiredService<ApplicationDbContext>();
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
                    catch (Exception)
                    {
                        // Database/tenant failures cannot bypass session revocation.
                        context.Fail("Session could not be validated.");
                    }
                    finally
                    {
                        context.HttpContext.User = previousPrincipal;
                    }
                }
            };
        });


        return services;
    }
}
