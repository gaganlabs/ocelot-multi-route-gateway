using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gateway.Configurators
{
    public static class AlterUpstream
    {
        public static string ConfigureUpstreamSecurity(HttpContext context, string swaggerJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(swaggerJson))
                {
                    return swaggerJson;
                }

                var swagger = JObject.Parse(swaggerJson);
                var componets = swagger.GetValue("components");
                
                // Create components section if it doesn't exist
                if (componets == null)
                {
                    swagger["components"] = new JObject();
                    componets = swagger["components"];
                }

                if (componets == null)
                {
                    return swaggerJson;
                }

                var bearer = new JObject
                {
                    { "type", "http" },
                    { "description", "JWT Authorization header using the Bearer scheme. Enter token only. " },
                    { "scheme", "bearer" },
                    { "bearerFormat", "JWT" }
                };

                componets["securitySchemes"] = new JObject
                {
                    { "Bearer", bearer }
                };

                swagger["security"] = new JArray(
                    new JObject
                    {
                        { "Bearer", new JArray() }
                    }
                );

                return swagger.ToString(Formatting.Indented);
            }
            catch (Exception)
            {
                // If parsing fails, return original JSON
                return swaggerJson;
            }
        }
    }
}

