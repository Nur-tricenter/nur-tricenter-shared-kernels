using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Nur.Tricenter.Security;

/// <summary>
/// Wires JWT bearer validation and one authorization policy per permission, so a controller asks
/// for <c>[Authorize(Policy = "catalog.food:edit")]</c> rather than hand-rolling a scheme.
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Registers the bearer scheme and a named policy per entry in <paramref name="permissions"/>.
    /// </summary>
    /// <remarks>
    /// The parameter is named <c>permissions</c>, not <c>scopes</c>. Semantically it is the list of
    /// permissions the service recognises: each becomes a policy that demands the token's
    /// <c>scope</c> claim to contain that exact string. A service that has no permissions still
    /// registers the scheme — an authenticated endpoint with no scopes is a valid configuration.
    /// </remarks>
    public static IServiceCollection AddSecurity(
        this IServiceCollection services,
        JwtOptions jwtOptions,
        IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(jwtOptions);

        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
        {
            throw new InvalidOperationException(
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.SecretKey)} has no value. " +
                "Supply it through the Jwt__SecretKey environment variable, or a local git-ignored " +
                "appsettings.Development.json. A signing key does not belong in version control.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = jwtOptions.ValidateIssuer,
                    ValidIssuer = jwtOptions.ValidIssuer,
                    ValidateAudience = jwtOptions.ValidateAudience,
                    ValidAudience = jwtOptions.ValidAudience,
                    ValidateLifetime = jwtOptions.ValidateLifetime,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Anonymous, policy => policy.RequireAssertion(_ => true));

        foreach (var permission in permissions)
        {
            services.AddAuthorizationBuilder()
                .AddPolicy(permission, policy => policy.RequireAssertion(
                    context => context.User
                        .FindAll("scope")
                        .Select(claim => claim.Value)
                        .Any(value => value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                         .Contains(permission, StringComparer.Ordinal))));
        }

        return services;
    }
}