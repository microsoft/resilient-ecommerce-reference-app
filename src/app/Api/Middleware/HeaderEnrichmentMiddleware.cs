namespace Api.Middleware
{
    /// <summary>
    /// ASP.Net middleware that enriches the response data with additional headers
    /// containing information about the application gateway, node IP, and pod name
    /// used to handle the request.
    /// </summary>
    public class HeaderEnrichmentMiddleware
    {
        private readonly RequestDelegate _next;

        public HeaderEnrichmentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var appGatewayIp = context.Request.Headers.FirstOrDefault(h => h.Key == "X-Forwarded-For");
            var nodeIp = Environment.GetEnvironmentVariable("MY_NODE_IP");
            var podName = Environment.GetEnvironmentVariable("MY_POD_NAME");

            context.Response.Headers.Append("AzRef-AppGwIp", appGatewayIp.Value.FirstOrDefault());
            context.Response.Headers.Append("AzRef-NodeIp", nodeIp);
            context.Response.Headers.Append("AzRef-PodName", podName);

            await _next(context);
        }
    }
}
