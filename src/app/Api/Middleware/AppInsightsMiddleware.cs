using Api.Telemetry;
using Microsoft.ApplicationInsights;

namespace Api.Middleware
{
    /// <summary>
    /// ASP.Net factory-based middleware that runs as part of the execution chain of each processed API request.
    /// This middleware captures and reports custom telemetry data to Azure Application Insights.
    /// 
    /// This middleware needs to be registered as a scoped/transient service in the DI container
    /// as it requires a new instance of the TelemetryClient.
    /// </summary>
    public class AppInsightsMiddleware : IMiddleware
    {
        private readonly TelemetryClient _telemetryClient;

        public AppInsightsMiddleware(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            string actionName = context.Request.RouteValues["action"]?.ToString() ?? "Unknown";
            _telemetryClient.CaptureEvent(actionName, context.Request, context.Response);

            await next.Invoke(context);
        }
    }
}
