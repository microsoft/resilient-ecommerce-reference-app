namespace Api.Telemetry
{
    /// <summary>
    /// POCO representing the custom model of telemetry data reported
    /// to Azure Application Insights for each processed API request.
    /// </summary>
    public class OperationBreadcrumb
    {
        public required string OperationStatus { get; set; }
        public required string OperationName { get; set; }
        public string? SessionId { get; set; }
        public string? RequestId { get; set; }
        public int RetryCount { get; internal set; }
        public InfrastructureMetadata? Infrastructure { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Status", OperationStatus },
                { "OperationName", OperationName },
                { "SessionID", SessionId ?? string.Empty },
                { "RequestID", RequestId ?? string.Empty },
                { "RetryCount", RetryCount.ToString() },
                { "Infrastructure", Infrastructure.Serialize() }
            };
        }
    }
}
