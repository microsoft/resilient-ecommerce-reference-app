using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Api.Infrastructure.Datastore;

namespace Api.Infrastructure
{
    /// <summary>
    /// Executes a simple heartbeat check against the configured database context, in case of usage of Entity Framework.
    /// Can be run as a Hangfire job.
    /// </summary>
    public class DatabaseContextHealthCheck : IHealthCheck
    {
        private readonly ILogger<DatabaseContextHealthCheck> _logger;
        private readonly DatabaseContext _concertDataContext;

        public DatabaseContextHealthCheck(ILogger<DatabaseContextHealthCheck> logger, DatabaseContext concertDataContext)
        {
            _logger = logger;
            _concertDataContext = concertDataContext;
        }

        /// <summary>
        /// Executes the health check against the database context, logging the total number
        /// of rows for each table in the database.
        /// </summary>
        /// <param name="context">the health check context of the current operation</param>
        /// <param name="cancellationToken">cancellation token of the async operation</param>
        /// <returns>whether the heartbeat is healthy or unhealthy</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var ticketCount = await _concertDataContext.Orders.AsNoTracking().CountAsync();
                var orderCount = await _concertDataContext.Orders.AsNoTracking().CountAsync();
                var concertCount = await _concertDataContext.Concerts.AsNoTracking().CountAsync();
                var userCount = await _concertDataContext.Users.AsNoTracking().CountAsync();

                _logger.LogInformation($"Ticket count: {orderCount}");
                _logger.LogInformation($"Order count: {orderCount}");
                _logger.LogInformation($"Concert count: {concertCount}");
                _logger.LogInformation($"User count: {userCount}");

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("SQL Database is unhealthy.", ex);
            }
        }
    }
}
