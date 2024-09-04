using Api;
using System.Configuration;

// Load .env values
DotNetEnv.Env.Load();

// Configure webapp builder. Enable developers to override settings with user secrets
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);
builder.Logging.AddConsole();

// Configure DI services
var logger = LoggerFactory.Create(loggingBuilder =>
{
    loggingBuilder.AddConfiguration(builder.Configuration.GetSection("Logging"));
}).CreateLogger<Startup>();

var startup = new Startup(builder.Configuration, logger);
startup.ConfigureServices(builder.Services);

// Start the application
var app = builder.Build();
startup.Configure(app, app.Environment);

app.Run();
