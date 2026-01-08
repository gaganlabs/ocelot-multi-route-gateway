using Microsoft.Extensions.Configuration;
using Ocelot.Configuration.File;

namespace Gateway.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring Ocelot file-based route configurations.
    /// This class enables dynamic resolution of placeholder hostnames in route definitions,
    /// allowing route files to use environment-agnostic placeholders (e.g., "{Identity}", "{MailDelivery}")
    /// that are resolved at runtime from the GlobalHosts configuration section in appsettings.json.
    /// 
    /// This approach provides several benefits:
    /// - Environment flexibility: Route files remain unchanged across dev/staging/prod environments
    /// - Centralized configuration: Service URLs are managed in a single location (appsettings.json)
    /// - Maintainability: Service endpoint changes only require updating appsettings.json, not multiple route files
    /// </summary>
    public static class FileConfigurationExtensions
    {
        /// <summary>
        /// Resolves placeholder hostnames in Ocelot routes from the GlobalHosts configuration.
        /// Uses PostConfigure to modify the FileConfiguration after Ocelot loads route files from the Routes folder.
        /// 
        /// Placeholders in route files (e.g., "Host": "{Identity}") are replaced with actual URIs
        /// from the GlobalHosts section (e.g., "Identity": "https://localhost:7234"), setting the
        /// downstream scheme, host, and port accordingly.
        /// </summary>
        /// <param name="configurationBuilder">The configuration builder to extend.</param>
        /// <param name="configuration">The configuration instance containing GlobalHosts.</param>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        public static IConfigurationBuilder ResolveDownstreamHostPlaceholders(
            this IConfigurationBuilder configurationBuilder, IConfiguration configuration, IServiceCollection services)
        {
            services.PostConfigure<FileConfiguration>(fileConfiguration =>
            {
                var globalHosts = configuration.GetSection("GlobalHosts").Get<GlobalHosts>();

                if (globalHosts != null)
                {
                    foreach (var route in fileConfiguration.Routes)
                    {
                        ConfigureRoute(route, globalHosts);
                    }
                }
            });

            return configurationBuilder;
        }

        /// <summary>
        /// Configures a single route by resolving placeholder hostnames to actual URIs from GlobalHosts.
        /// If a route's downstream host contains a placeholder (e.g., "{Identity}"), it is replaced
        /// with the corresponding URI from the GlobalHosts dictionary, updating the scheme, host, and port.
        /// </summary>
        /// <param name="route">The Ocelot route to configure.</param>
        /// <param name="globalHosts">The dictionary of service names to URIs for placeholder resolution.</param>
        private static void ConfigureRoute(FileRoute route, GlobalHosts globalHosts)
        {
            foreach (var hostAndPort in route.DownstreamHostAndPorts)
            {
                var host = hostAndPort.Host;
                if (host.StartsWith("{") && host.EndsWith("}"))
                {
                    var placeHolder = host.TrimStart('{').TrimEnd('}');
                    if (globalHosts.TryGetValue(placeHolder, out var uri))
                    {
                        route.DownstreamScheme = uri.Scheme;
                        hostAndPort.Host = uri.Host;
                        hostAndPort.Port = uri.Port;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents a dictionary mapping service name placeholders to their corresponding URIs.
    /// This class is used to deserialize the "GlobalHosts" configuration section from appsettings.json,
    /// where keys are service identifiers (e.g., "Identity", "OrderService") and values are the
    /// complete URIs (scheme, host, and port) for those services.
    /// </summary>
     public class GlobalHosts : Dictionary<string, Uri> { }
}