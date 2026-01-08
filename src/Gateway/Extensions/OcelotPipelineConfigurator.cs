using Ocelot.Middleware;

namespace Gateway.Extensions
{
    /// <summary>
    /// Configures the Ocelot pipeline middleware for the gateway.
    /// </summary>
    public static class OcelotPipelineConfigurator
    {
        /// <summary>
        /// Creates and configures the Ocelot pipeline configuration with custom middleware.
        /// </summary>
        /// <returns>An <see cref="OcelotPipelineConfiguration"/> instance with pre-authorization middleware configured to process claims.</returns>
        public static OcelotPipelineConfiguration CreatePipelineConfiguration()
        {
            var ocelotConfig = new OcelotPipelineConfiguration
            {
                PreAuthorizationMiddleware = async (ctx, next) =>
                {
                    await ClaimsProcessor.ProcessClaims(ctx, next);
                }
            };

            return ocelotConfig;
        }
    }
}
