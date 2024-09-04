using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Api.Infrastructure.Datastore;

namespace Api.Infrastructure
{
    /// <summary>
    /// Class that handles the cleanup of the database.
    /// Can be registered as a Hangfire background cron job to run at a specified interval.
    /// This specific implementation clears a configuration-specified number of rows
    /// that are older than a configuration-specified number of minutes.
    /// See "DATABASE_CLEANUP_RECORD_COUNT" and "DATABASE_CLEANUP_THRESHOLD_MINUTES" in the configuration file of the app.
    /// </summary>
    public class DatabaseCleanupJob : IDatabaseCleanupJob
    {
        // See: https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors?view=sql-server-ver16
        private const int SQL_ERROR_DEADLOCK = 1205;

        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<DatabaseCleanupJob> _logger;
        private readonly DatabaseContext _dataContext;
        private readonly int _cleanupBatchSize;
        private readonly int _recordAgeThreshold;

        public DatabaseCleanupJob(
            TelemetryClient telemetryClient, 
            ILogger<DatabaseCleanupJob> logger, 
            IConfiguration configuration, 
            DatabaseContext dataContext
            )
        {
            _telemetryClient = telemetryClient;
            _logger = logger;
            _dataContext = dataContext;

            _cleanupBatchSize = int.Parse(configuration["DATABASE_CLEANUP_RECORD_COUNT"] ?? "1000");
            _recordAgeThreshold = int.Parse(configuration["DATABASE_CLEANUP_THRESHOLD_MINUTES"] ?? "30");
        }

        /// <inheritdoc />
        public async Task ExecuteAsync()
        {
            try
            {
                var cutOffThreshold = -1 * _recordAgeThreshold;
                var cutOffTime = DateTime.UtcNow.AddMinutes(cutOffThreshold);
                var count = _cleanupBatchSize;

                var eventAtts = new Dictionary<string, string>();
                eventAtts.Add("Count", _cleanupBatchSize.ToString());
                _telemetryClient.TrackEvent("DATABASE_CLEANUP_ATTEMPT", eventAtts);

                var timeout = 180; // Set the timeout to 180 seconds (3 minutes)
                _dataContext.Database.SetCommandTimeout(timeout);

                // Tickets
                var ticketsDeleted = await DeleteOldTicketsAsync();

                // Users
                var usersDeleted = await DeleteOldUsersAsync();

                var eventAtts2 = new Dictionary<string, string>();
                eventAtts2.Add("BatchSize", _cleanupBatchSize.ToString());
                eventAtts2.Add("UsersDeleted", usersDeleted.ToString());
                _telemetryClient.TrackEvent("DATABASE_CLEANUP", eventAtts2);
            }
            catch (Exception ex)
            {
                if (ex is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    bool isDeadlock = false;

                    foreach (Microsoft.Data.SqlClient.SqlError error in sqlEx.Errors)
                    {
                        // This IS going to happen due to how this was implemented and the number of instances running.
                        // This code should be refactored so that we don't have multiple instances of the AKS pods running cleanup
                        if (error.Number == SQL_ERROR_DEADLOCK)
                        {
                            isDeadlock = true;
                            _logger.LogInformation("Cleanup job deadlock encountered");
                            break;
                        }
                    }

                    if (!isDeadlock)
                    {
                        _logger.LogError(ex, "Unhandled exception from DatabaseCleanupJob.ExecuteAsync");
                    }
                }
                else
                {
                    _logger.LogError(ex, "Unhandled exception from DatabaseCleanupJob.ExecuteAsync");
                }
            }
        }

        /// <summary>
        /// Executes the cleanup strategy for the "Tickets" table.
        /// </summary>
        /// <returns>the total number of rows deleted</returns>
        private async Task<int> DeleteOldTicketsAsync() 
        {
            var cutOffTime = DateTime.UtcNow.AddMinutes(-1 * _recordAgeThreshold);
            var totalDeleted = 0;
            while (true)
            {
                var rowsAffected = await _dataContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Tickets WHERE Id IN (SELECT TOP (@p0) Id FROM Tickets WHERE CreatedDate < @p1 ORDER BY CreatedDate ASC)", 
                    _cleanupBatchSize, cutOffTime
                );

                if (rowsAffected == 0)
                {
                    break;
                }

                totalDeleted += rowsAffected; // Update the total number of records deleted
            }
            return totalDeleted;
        }

        /// <summary>
        /// Executes the cleanup strategy for the "Users" table.
        /// </summary>
        /// <returns>the total number of rows deleted</returns>
        private async Task<int> DeleteOldUsersAsync() 
        {
            var cutOffTime = DateTime.UtcNow.AddMinutes(-1 * _recordAgeThreshold);
            var totalDeleted = 0;
            while (true)
            {
                var rowsAffected = await _dataContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Users WITH (READPAST) WHERE Id IN (SELECT TOP (@p0) Id FROM Users WHERE CreatedDate < @p1 ORDER BY CreatedDate ASC)", 
                    _cleanupBatchSize, 
                    cutOffTime
                );

                if (rowsAffected == 0)
                {
                    break;
                }

                totalDeleted += rowsAffected; // Update the total number of records deleted
            }
            return totalDeleted;
        }
    }
}
