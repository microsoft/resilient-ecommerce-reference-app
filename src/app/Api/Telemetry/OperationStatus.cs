namespace Api.Telemetry
{
    /// <summary>
    /// Available operation statuses reported to telemetry. Represent the possible
    /// outcomes of a processed API request.
    /// </summary>
    public class OperationStatus
    {
        private OperationStatus(string value) { Value = value; }

        public string Value { get; private set; }

        public static OperationStatus Ok { get { return new OperationStatus("OK"); } }
        public static OperationStatus ClientError { get { return new OperationStatus("CLIENT_ERROR"); } }
        public static OperationStatus ServerError { get { return new OperationStatus("SERVER_ERROR"); } }

        public static OperationStatus FromResponse(HttpResponse response)
        {
            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                return Ok;
            }
            if (response.StatusCode >= 400 && response.StatusCode < 500)
            {
                return ClientError;
            }
            return ServerError;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
