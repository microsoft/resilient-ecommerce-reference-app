using StackExchange.Redis;
using Azure.Identity;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using AutoMapper;
using Api.Infrastructure;
using Hangfire;
using Hangfire.MemoryStorage;
using Api.Services.Repositories;
using Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Api.Infrastructure.Datastore;
using Api.Middleware;

namespace Api
{
    public class Startup
    {
        private readonly bool _isDevelopment;
        private readonly bool _useSelfHostedDatastore;

        private readonly IConfiguration _configuration;
        private readonly ILogger<Startup> _logger;

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            _logger = logger;

            _isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;
            _useSelfHostedDatastore = Environment.GetEnvironmentVariable("USE_SELF_HOSTED_DATASTORE") != null;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options => options.SuppressAsyncSuffixInActionNames = false);

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddHealthChecks();

            // Configure Hangfire
            services.AddHangfire(config => config.UseMemoryStorage());
            services.AddHangfireServer();

            // Configure Azure Application Insights for capturing telemetry
            var aiOptions = new ApplicationInsightsServiceOptions();
            aiOptions.EnableAdaptiveSampling = false;
            aiOptions.EnableQuickPulseMetricStream = false;

            services.AddApplicationInsightsTelemetry(aiOptions);

            // Configure AutoMapper with our DTO <-> Entity mappings
            var mapperConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new MappingProfile());
            });

            IMapper mapper = mapperConfig.CreateMapper();
            services.AddSingleton(mapper);

            // Configure Azure Redis cache
            AddAzureCacheForRedis(services);

            // Configure DB Context
            var maxRetryCount = int.Parse(Configuration["RPOL_CONNECT_RETRY"] ?? "3");
            var maxRetryDelay = int.Parse(Configuration["RPOL_BACKOFF_DELTA"] ?? "1500");

            var sqlConnectionString = GetSqlConnectionString();

            services.AddDbContextPool<DatabaseContext>(options => options.UseSqlServer(sqlConnectionString,
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: maxRetryCount,
                    maxRetryDelay: TimeSpan.FromMilliseconds(maxRetryDelay),
                    errorNumbersToAdd: null);
                }));

            // Register factory-based middlewares as scoped services inside the DI container
            services.AddScoped<AppInsightsMiddleware>();

            // Register the app's services
            services.AddScoped<ICartRepository, CachedCartRepository>();
            services.AddScoped<IConcertRepository, CachedConcertRepository>();
            services.AddScoped<ITicketRepository, TicketRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<TicketPurchasingService, NonAtomicTicketPurchasingService>();
            services.AddScoped<DatabaseContextHealthCheck>();

            // Add health checks for our DBs
            services.AddHealthChecks()
                .AddCheck<RedisCacheHealthCheck>("redis")
                .AddCheck<SqlDbHealthCheck>("sql");

            services.AddScoped<IDatabaseCleanupJob, DatabaseCleanupJob>();
            services.AddScoped<ApplicationInitializer, ApplicationInitializer>();
        }

        private void AddAzureCacheForRedis(IServiceCollection services)
        {
            ConfigurationOptions redisOptions = GetRedisConfiguration();

            services.AddSingleton<IConnectionMultiplexer>(sp => {
                var connection = ConnectionMultiplexer.Connect(redisOptions);
                return connection;
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = redisOptions;
            });
        }

        private ConfigurationOptions GetRedisConfiguration()
        {
            var serviceEndpoint = Environment.GetEnvironmentVariable("REDIS_ENDPOINT");

            if (string.IsNullOrEmpty(serviceEndpoint))
            {
                throw new Exception("No 'REDIS_ENDPOINT' set. Can't connect to any Redis instance.");
            }

            var serviceEndpointWithPort = serviceEndpoint.Contains(':') ? serviceEndpoint : $"{serviceEndpoint}:6380";
            var configurationOptions = ConfigurationOptions.Parse(serviceEndpointWithPort);

            if (!_useSelfHostedDatastore)
            {
                _logger.LogInformation("Using Managed Identity to authenticate against Redis.");

                configurationOptions = configurationOptions.ConfigureForAzureWithTokenCredentialAsync(
                    new DefaultAzureCredential(includeInteractiveCredentials: _isDevelopment)).GetAwaiter().GetResult();
            }

            return configurationOptions;
        }

        /// <summary>
        /// Returns the SQL connection string used to connect to the SQL database. The connection
        /// configuration is based on the environment variables set in the app's configuration.
        /// Authorization is done using:
        ///   - Managed Identity by default -- when the datastore is deployed in Azure
        ///   - Interactive login if running in development mode -- when the datastore is deployed in Azure, but app is running locally
        ///   - SQL Server username & password if the datastore is self-hosted (in VMs/containers)
        /// </summary>
        /// <returns>the managed connection string used to connect to the SQL database, using the most restrictive authorization configuration</returns>
        /// <exception cref="Exception">if the app is misconfigured (missing configurations)</exception>
        private string GetSqlConnectionString()
        {
            var sqlEndpoint = Environment.GetEnvironmentVariable("SQL_ENDPOINT");
            var sqlAppDatabaseName = Environment.GetEnvironmentVariable("SQL_APP_DATABASE_NAME");
            var sqlUser = Environment.GetEnvironmentVariable("SQL_USER");
            var sqlPassword = Environment.GetEnvironmentVariable("SQL_PASSWORD");

            if (string.IsNullOrEmpty(sqlEndpoint) || string.IsNullOrEmpty(sqlAppDatabaseName))
            {
                throw new Exception("No 'SQL_ENDPOINT' or 'SQL_APP_DATABASE_NAME' set. Can't connect to any SQL instance.");
            }

            if (_useSelfHostedDatastore && (string.IsNullOrEmpty(sqlUser) || string.IsNullOrEmpty(sqlPassword)))
            {
                throw new Exception("App is configured to run with self hosted datastores but no 'SQL_USER' and/or 'SQL_PASSWORD' set.");
            }


            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = sqlEndpoint,
                InitialCatalog = sqlAppDatabaseName,
                Authentication = SqlAuthenticationMethod.ActiveDirectoryDefault,
                Encrypt = true,
                TrustServerCertificate = false
            };

            if (_isDevelopment)
            {
                _logger.LogWarning("Running in development mode. Allowing for interactive Entra ID login.");

                sqlConnectionStringBuilder.Encrypt = false;
                sqlConnectionStringBuilder.TrustServerCertificate = true;
                sqlConnectionStringBuilder.Authentication = SqlAuthenticationMethod.ActiveDirectoryInteractive;
            }

            if (_useSelfHostedDatastore)
            {
                _logger.LogWarning("Using a self-hosted SQL Server. Connecting through server username & password.");

                sqlConnectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
                sqlConnectionStringBuilder.UserID = sqlUser;
                sqlConnectionStringBuilder.Password = sqlPassword;
            }

            return sqlConnectionStringBuilder.ConnectionString;
        }

        public void Configure(WebApplication app, IWebHostEnvironment env)
        {
            // Migrate DB and seed data
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                services.GetRequiredService<ApplicationInitializer>().Initialize();
            }

            // Setup API Swagger documentation
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "api/swagger/{documentName}/swagger.json";
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "ECommerce API V1");
                c.RoutePrefix = "api/swagger";
            });

            using var serviceScope = app.Services.CreateScope();
            
            // Configure the HTTP request pipeline.
            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Register middleware
            app.UseMiddleware<HeaderEnrichmentMiddleware>();
            app.UseMiddleware<AppInsightsMiddleware>();

            // Setup controllers, along with a health check endpoint
            app.UseRouting();
            app.MapControllers();

            app.MapGet("/api/live", async context =>
            {
                await context.Response.WriteAsync("Healthy");
            });

            app.UseHangfireDashboard();
        }
    }
}
