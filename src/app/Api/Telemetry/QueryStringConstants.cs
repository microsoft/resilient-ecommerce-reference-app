using System.Web;

namespace Api.Telemetry
{
    /// <summary>
    /// Static class enhancing the HttpRequest object with utility methods
    /// for processing & extracting query string parameters.
    /// </summary>
    public static class QueryStringConstants
    {
        public const string SESSION_ID = "AZREF_SESSION_ID";
        public const string REQUEST_ID = "AZREF_REQUEST_ID";
        public const string RETRY_COUNT = "AZREF_RETRY_COUNT";

        public static string ExtractQueryString(this HttpRequest request, string queryParamKey)
        {
            string? headerValue = null;
            if (request.QueryString.HasValue)
            {
                var queryParams = HttpUtility.ParseQueryString(request.QueryString.Value);
                headerValue = queryParams[queryParamKey];
            }
            return headerValue ?? string.Empty;
        }

        public static int ExtractIntegerFromQueryString(this HttpRequest request, string queryParamKey)
        {
            string? headerValue = null;
            if (request.QueryString.HasValue)
            {
                var queryParams = HttpUtility.ParseQueryString(request.QueryString.Value);
                headerValue = queryParams[queryParamKey];
            }

            int.TryParse(headerValue ?? "-1", out var hearderIntValue);
            return hearderIntValue;
        }
    }
}
