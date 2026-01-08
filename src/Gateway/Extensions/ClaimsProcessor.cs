using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using System.Security.Claims;

namespace Gateway.Extensions
{
    /// <summary>
    /// A utility class that processes claims from authenticated users and formats them as HTTP headers for downstream requests.
    /// Transforms standard and SSO-specific claims (e.g., Okta) into standardized header names:
    /// - NameIdentifier -> X-User-Sid
    /// - Name -> X-User-Name
    /// - Email -> X-User-Email
    /// - Role -> X-User-Role
    /// - Custom claims -> X-Claim-{ClaimType}
    /// The processed claims are added as headers to the Ocelot downstream request, enabling downstream services
    /// to access user identity information without requiring re-authentication.
    /// </summary>
    public static class ClaimsProcessor
    {
        /// <summary>
        /// A middleware function that processes the claims from the authenticated user.
        /// It formats claims and adds them as headers to the downstream request.
        /// </summary>
        public static Func<HttpContext, Func<Task>, Task> ProcessClaims
        => async (context, next) =>
        {
            // Check if user is authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                // Get the downstream request from Ocelot
                if (context.Items.DownstreamRequest() is DownstreamRequest downstreamRequest)
                {
                    // Process claims and convert them to headers
                    var requestHeaders = ProcessClaimsToHeaders(context.User.Claims);

                    // Add processed claims as headers to the downstream request
                    foreach (var header in requestHeaders)
                    {
                        // Remove existing header if present, then add the new one
                        downstreamRequest.Headers.Remove(header.Key);
                        downstreamRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }
            
            await next.Invoke();
        };

        /// <summary>
        /// Processes user claims and converts them to HTTP headers for downstream requests.
        /// Transforms standard claim types to standardized header names (e.g., NameIdentifier -> X-User-Sid for Okta SSO).
        /// Handles multiple values for the same claim type by comma-separating them.
        /// </summary>
        /// <param name="claims">The collection of claims from the authenticated user.</param>
        /// <returns>A dictionary of header key-value pairs to add to the downstream request.</returns>
        private static Dictionary<string, string> ProcessClaimsToHeaders(IEnumerable<Claim> claims)
        {
            var headers = new Dictionary<string, string>();
            
            foreach (var claim in claims)
            {
                string headerKey = claim.Type;
                string headerValue = claim.Value;

                // Transform SSO-specific claims
                // Handle Okta NameIdentifier -> sid transformation
                if (claim.Type == ClaimTypes.NameIdentifier || claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                {
                    headerKey = "X-User-Sid"; // or "sid" depending on your downstream service requirements
                }
                // Handle other common claim transformations
                else if (claim.Type == ClaimTypes.Name)
                {
                    headerKey = "X-User-Name";
                }
                else if (claim.Type == ClaimTypes.Email)
                {
                    headerKey = "X-User-Email";
                }
                else if (claim.Type == ClaimTypes.Role)
                {
                    headerKey = "X-User-Role";
                }
                // For custom claims, prefix with X-Claim- to avoid conflicts
                else if (!claim.Type.StartsWith("X-") && !claim.Type.StartsWith("http://"))
                {
                    headerKey = $"X-Claim-{claim.Type.Replace(" ", "-")}";
                }

                // Add or append to existing header if multiple values exist
                if (headers.ContainsKey(headerKey))
                {
                    headers[headerKey] = $"{headers[headerKey]}, {headerValue}";
                }
                else
                {
                    headers[headerKey] = headerValue;
                }
            }

            return headers;
        }
    }
}
