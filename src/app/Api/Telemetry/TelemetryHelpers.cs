using Microsoft.ApplicationInsights;
using System.Text.Json;

namespace Api.Telemetry
{
    /// Static class enhancing the TelemetryClient object with utility methods
    /// for capturing and reporting custom telemetry data to Azure Application Insights.
    /// </summary>
    public static class TelemetryHelpers
    {
        public static void CaptureEvent (this TelemetryClient telemetryClient, string operationName, HttpRequest request, HttpResponse response)
        {
            var operationStatus = OperationStatus.FromResponse(response);
            var breadCrumb = new OperationBreadcrumb()
            {
                OperationStatus = operationStatus.Value,
                OperationName = operationName,
                SessionId = request.ExtractQueryString(QueryStringConstants.SESSION_ID),
                RequestId = request.ExtractQueryString(QueryStringConstants.REQUEST_ID),
                RetryCount = request.ExtractIntegerFromQueryString(QueryStringConstants.RETRY_COUNT),
                Infrastructure = InfrastructureMetadata.FromHttpRequest(request)
            };

            telemetryClient.TrackEvent(breadCrumb.OperationName, breadCrumb.ToDictionary());
        }

        public static string Serialize<T>(this T objectData)
        {
            return JsonSerializer.Serialize(objectData);
        }
    }
}
