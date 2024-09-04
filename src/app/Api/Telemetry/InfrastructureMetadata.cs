using Microsoft.Extensions.Primitives;

namespace Api.Telemetry
{
    /// <summary>
    /// POCO representing custom infrastructure information to report as telemetry
    /// to Azure Application Insights for each processed API request.
    /// </summary>
    public class InfrastructureMetadata
    {
        public string? Namespace { get; set; }
        public string? PodName { get; set; }
        public string? PodIP { get; set; }
        public string? ServiceAccount { get; set; }
        public string? NodeName { get; set; }
        public string? NodeIP { get; set; }
        public string? AppGatewayIP { get; set; }

        public static InfrastructureMetadata FromHttpRequest(HttpRequest request)
        {
            request.Headers.TryGetValue("X-Forwarded-For", out StringValues appGwIps);

            var metadata = new InfrastructureMetadata
            {
                NodeName = Environment.GetEnvironmentVariable("MY_NODE_NAME"),
                NodeIP = Environment.GetEnvironmentVariable("MY_NODE_IP"),
                PodName = Environment.GetEnvironmentVariable("MY_POD_NAME"),
                Namespace = Environment.GetEnvironmentVariable("MY_POD_NAMESPACE"),
                PodIP = Environment.GetEnvironmentVariable("MY_POD_IP"),
                ServiceAccount = Environment.GetEnvironmentVariable("MY_POD_SERVICE_ACCOUNT"),
                AppGatewayIP = appGwIps.FirstOrDefault()
            };

            return metadata;
        }
    }
}
