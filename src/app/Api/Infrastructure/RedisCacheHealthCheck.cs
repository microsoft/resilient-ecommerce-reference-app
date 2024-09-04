using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Api.Infrastructure
{
    /// <summary>
    /// Executes a simple heartbeat check against the configured Redis Cache instance.
    /// Can be run as a Hangfire job.
    /// </summary>
    public class RedisCacheHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisCacheHealthCheck(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        }

        /// <summary>
        /// Executes the health check against the Redis Cache instance.
        /// The heartbeat check is a simple PING command.
        /// </summary>
        /// <param name="context">the health check context of the current operation</param>
        /// <param name="cancellationToken">cancellation token of the async operation</param>
        /// <returns>whether the heartbeat is healthy or unhealthy</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var database = _connectionMultiplexer.GetDatabase();

                var result = await database.ExecuteAsync("PING");
                if (result.IsNull)
                {
                    return HealthCheckResult.Unhealthy("Redis did not respond to PING.");
                }
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Redis connectivity check failed.", ex);
            }
        }
    }
}
