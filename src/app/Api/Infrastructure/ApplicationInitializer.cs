using Api.Infrastructure.Datastore;
using Hangfire;
using Microsoft.ApplicationInsights;

namespace Api.Infrastructure
{
    /// <summary>
    /// Initializer class for the application. Built and initialized at app startup,
    /// the class initializes all the services required for the app to run.
    /// Also starts, if configured, all the specified recurring jobs.
    /// </summary>
    public class ApplicationInitializer
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IConfiguration _configuration;
        private readonly IDatabaseCleanupJob _databaseCleanupJob;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly DatabaseContext _dbContext;

        public ApplicationInitializer(
            TelemetryClient telemetryClient, 
            IConfiguration configuration,
            IDatabaseCleanupJob databaseCleanupJob, 
            IRecurringJobManager recurringJobManager,
            DatabaseContext dbContext)
        {
            _telemetryClient = telemetryClient;
            _configuration = configuration;
            _databaseCleanupJob = databaseCleanupJob;
            _recurringJobManager = recurringJobManager;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Should be called at application startup to initialize all services
        /// and start all background cron jobs.
        /// </summary>
        public void Initialize()
        {
            // Initialize datastore at application startup
            _dbContext.Initialize();

            _telemetryClient.TrackEvent("DATABASE_INITIALIZED");

            var isDatabaseCleanupEnabled = bool.Parse(_configuration?["DATABASE_CLEANUP_ENABLED"] ?? "false");
            if (isDatabaseCleanupEnabled) 
            {
                _recurringJobManager.AddOrUpdate<DatabaseCleanupJob>("database-cleanup", job => _databaseCleanupJob.ExecuteAsync(), Cron.Minutely);
            
                _telemetryClient.TrackEvent("DATABASE_CLEANUP_SETUP");
            }
        }
    }
}
