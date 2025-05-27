using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Funcy.Console;
using Funcy.Console.Concurrency;
using Funcy.Console.Handlers;
using Funcy.Console.Ui;
using Funcy.Core.Interfaces;
using Funcy.Data;
using Funcy.Infrastructure.Azure;
using Funcy.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSerilog();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddMemoryCache();
        services.AddDbContextFactory<FunctionAppDbContext>(options =>
        {
            options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection"))
                .UseLoggerFactory(LoggerFactory.Create(builder =>
                {
                    builder.AddSerilog().SetMinimumLevel(LogLevel.Information);
                })).EnableSensitiveDataLogging();
        });
        
        services.AddTransient<InputHandler>();
        services.AddTransient<FunctionAppUpdateHandler>();
        services.AddTransient<ResizeHandler>();
        services.AddTransient<FunctionActionHandler>();
        services.AddSingleton<FunctionStateCoordinator>();
        services.AddTransient<MainMenuService>();
        services.AddTransient<IFunctionAppRepository, FunctionAppRepository>();
        services.AddTransient<IAzureFunctionService, AzureFunctionService>();
        services.AddTransient<IFunctionAppManagementService, FunctionAppManagementService>();
        services.AddScoped<IAzureSubscriptionService, AzureSubscriptionService>();
        services.AddSingleton<TokenCredential, DefaultAzureCredential>();
        services.AddHttpClient<KuduApiClient>();
    })
    .Build();

// Hämta vår service från DI-containern
var mainMenuService = host.Services.GetRequiredService<MainMenuService>();
await mainMenuService.StartAsync();

await host.RunAsync();

// 1. Keep `AzureFunctionService` focused on fetching function-app details and updating the local database.
// 2. Introduce a new service (e.g., `FunctionAppManagementService`) within `Funcy.Infrastructure/Azure` that handles runtime operations such as starting, stopping, and swapping slots.
// 3. Update DI registration in `src/Funcy.Console/Program.cs` to add the new service and adjust existing references.
// 4. Let the console UI or handlers use `FunctionAppManagementService` when invoking management commands, while still calling `AzureFunctionService` to refresh data.