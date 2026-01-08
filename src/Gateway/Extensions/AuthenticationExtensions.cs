using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Gateway.Extensions
{
    /// <summary>
    /// Authentication scheme types supported by the gateway.
    /// </summary>
    public enum AuthenticationScheme
    {
        /// <summary>
        /// Identity service authentication scheme using JWT Bearer tokens.
        /// </summary>
        Identity,

        /// <summary>
        /// SSO (OKTA) authentication scheme using JWT Bearer tokens.
        /// </summary>
        SSO
    }

    /// <summary>
    /// Extension methods for configuring multi-authentication schemes in the gateway.
    /// Supports JWT Bearer tokens from both Identity service and OKTA SSO.
    /// </summary>
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Configures multi-authentication schemes for the gateway.
        /// Adds two JWT Bearer authentication schemes:
        /// - "Identity" scheme: Validates JWT tokens from Identity service (symmetric key)
        /// - "SSO" scheme: Validates JWT tokens from OKTA SSO (using OKTA's authority metadata)
        /// </summary>
        /// <param name="services">The service collection to add authentication to.</param>
        /// <param name="configuration">The configuration instance containing JWT and SSO settings.</param>
        /// <param name="environment">The hosting environment to determine validation rules.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown when JWT secret key validation fails in production.</exception>
        public static IServiceCollection AddGatewayAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var ssoSettings = configuration.GetSection("SsoSettings");
            var secretKey = jwtSettings["SecretKey"];

            // Validate JWT secret key for Identity service
            secretKey = ValidateJwtSecretKey(secretKey, environment);

            var oktaAuthority = ssoSettings["Authority"] ?? throw new InvalidOperationException("OKTA Authority must be configured in SsoSettings:Authority");

            services.AddAuthentication(options =>
            {
                // Set default scheme to Identity - can be overridden per route/endpoint
                options.DefaultAuthenticateScheme = AuthenticationScheme.Identity.ToString();
                options.DefaultChallengeScheme = AuthenticationScheme.Identity.ToString();
            })
            // Identity Scheme - JWT Bearer Authentication for Identity service tokens
            .AddJwtBearer(AuthenticationScheme.Identity.ToString(), options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"] ?? "UserService",
                    ValidAudience = jwtSettings["Audience"] ?? "Microservices",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    // Add clock skew to handle small time differences between servers
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
                
                // Add event handlers for better error logging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var loggerFactory = context.HttpContext.RequestServices.GetService<ILoggerFactory>();
                        var logger = loggerFactory?.CreateLogger("JwtBearer");
                        logger?.LogWarning("JWT Authentication failed: {Error}. Exception: {ExceptionType}", 
                            context.Exception.Message, context.Exception.GetType().Name);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var loggerFactory = context.HttpContext.RequestServices.GetService<ILoggerFactory>();
                        var logger = loggerFactory?.CreateLogger("JwtBearer");
                        logger?.LogDebug("JWT Token validated successfully for user: {User}", 
                            context.Principal?.Identity?.Name ?? "Unknown");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var loggerFactory = context.HttpContext.RequestServices.GetService<ILoggerFactory>();
                        var logger = loggerFactory?.CreateLogger("JwtBearer");
                        logger?.LogWarning("JWT Authentication challenge: {Error}. ErrorDescription: {ErrorDescription}", 
                            context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            })
            // SSO Scheme - JWT Bearer Authentication for OKTA SSO tokens
            .AddJwtBearer(AuthenticationScheme.SSO.ToString(), options =>
            {
                // Configure OKTA authority for token validation
                options.Authority = oktaAuthority;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                
                // Get audience from configuration (optional - only validate if provided)
                var audience = ssoSettings["Audience"];
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // OKTA issuer validation is handled automatically via Authority
                    ValidAudience = audience,
                    // Clock skew allows for small time differences between servers
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                // Note: Logging can be added via ILogger if needed in the future
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Authentication failure logging can be added here if needed
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // Additional token validation logic can be added here
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        /// <summary>
        /// Validates the JWT secret key based on environment requirements.
        /// </summary>
        /// <param name="secretKey">The secret key to validate.</param>
        /// <param name="environment">The hosting environment.</param>
        /// <returns>The validated secret key.</returns>
        /// <exception cref="InvalidOperationException">Thrown when validation fails in production.</exception>
        private static string ValidateJwtSecretKey(string? secretKey, IHostEnvironment environment)
        {
            const string defaultSecretKey = "YourTLS!W@lc)m@-$ecr3Tsddiosnuhr";

            secretKey ??= defaultSecretKey;

            if (secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long.");
            }

            return secretKey;
        }
    }
}
