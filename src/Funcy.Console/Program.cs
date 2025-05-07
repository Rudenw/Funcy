using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Funcy.Console;
using Funcy.Console.Ui;
using Funcy.Console.Ui.Triggers;
using Funcy.Data;
using Funcy.Infrastructure.Azure;
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
        services.AddTransient<MainMenuService>();
        services.AddTransient<AzureFunctionService>();
        services.AddScoped<IAzureSubscriptionService, AzureSubscriptionService>();
        services.AddSingleton<TokenCredential, DefaultAzureCredential>();
        services.AddHttpClient<KuduApiClient>();
    })
    .Build();

// Hämta vår service från DI-containern
var mainMenuService = host.Services.GetRequiredService<MainMenuService>();
await mainMenuService.StartAsync();

await host.RunAsync();