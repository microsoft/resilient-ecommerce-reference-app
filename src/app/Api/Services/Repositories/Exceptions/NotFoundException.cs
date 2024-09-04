namespace Api.Services.Repositories.Exceptions
{
    /// <summary>
    /// Exception thrown by any repository implementation
    /// when the resource requested is not found.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message): base(message)
        { }
    }
}
