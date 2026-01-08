using Ocelot.Middleware;

namespace Gateway.Extensions
{
    /// <summary>
    /// A utility class that processes claims from the authenticated user and formats them for use in downstream requests.
    /// Specifically handles Single Sign-On (SSO) claims such as `NameIdentifier` from Okta, replacing it with `sid`
    /// in certain conditions and adding formatted claims as headers to an outgoing request.
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
            // var downstreamRequest = context.Items.DownstreamRequest();
            // var requestHeaders = context.ProcessClaims();
            // foreach (var header in requestHeaders)
            // {
            //     downstreamRequest.Headers.Add(header.Key, header.Value);
            // }
            await next.Invoke();
        };
    }
}
