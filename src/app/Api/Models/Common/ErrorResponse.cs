namespace Api.Models.Common
{
    /// <summary>
    /// HTTP response model the API returns when an error is encountered.
    /// Contains the error message and stack trace of the exception that occurred, if any.
    /// </summary>
    public class ErrorResponse
    {
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }

        public ErrorResponse(Exception ex)
        {
            ErrorMessage = ex.Message;
            StackTrace = ex.StackTrace ?? string.Empty;
        }

        public ErrorResponse(string errorMessage)
        {
            ErrorMessage = errorMessage;
            StackTrace = string.Empty;
        }
    }
}
