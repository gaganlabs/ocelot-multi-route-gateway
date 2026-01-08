using Ocelot.Middleware;

namespace Gateway.Extensions
{
    public static class OcelotPipelineConfigurator
    {
        public static OcelotPipelineConfiguration CreatePipelineConfiguration()
        {
            
            var ocelotConfig = new OcelotPipelineConfiguration
            {
                
                PreAuthorizationMiddleware = async (ctx, next) =>
                {
                    await ClaimsProcessor.ProcessClaims(ctx, next);
                },


               // AuthorizationMiddleware = async (httpContext, next) => await PermissionMiddleware.CreateAuthorizationFilter(httpContext, next)
            };

            return ocelotConfig;
        }
    }
}
