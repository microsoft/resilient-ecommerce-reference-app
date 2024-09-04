namespace Api.Infrastructure
{
    public interface IDatabaseCleanupJob
    {
        /// <summary>
        /// Executes the database cleanup job.
        /// </summary>
        Task ExecuteAsync();
    }
}
