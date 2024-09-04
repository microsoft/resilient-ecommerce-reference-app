using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Infrastructure
{
    /// <summary>
    /// Executes a simple heartbeat check against the configured SQL database.
    /// Can be run as a Hangfire job.
    /// </summary>
    public class SqlDbHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public SqlDbHealthCheck(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Runs a simple heartbeat check against the SQL Database instance
        /// </summary>
        /// <param name="context">the health check context of the current operation</param>
        /// <param name="cancellationToken">cancellation token of the async operation</param>
        /// <returns>whether the heartbeat is healthy or unhealthy</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync(cancellationToken);

                    using var command = new SqlCommand("SELECT 1 FROM [sys].[objects] WHERE 1=0;", connection);
                    await command.ExecuteScalarAsync(cancellationToken);
                }
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("SQL Database is unhealthy.", ex);
            }
        }
    }
}
