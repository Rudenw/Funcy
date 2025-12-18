using System.Text;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Funcy.Console;
using Funcy.Console.Handlers;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Ui;
using Funcy.Console.Ui.Factory;
using Funcy.Core.Interfaces;
using Funcy.Data;
using Funcy.Infrastructure.Azure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

Console.OutputEncoding = Encoding.UTF8;

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
            var connectionString = DatabaseConnectionFactory.CreateConnectionString(context.Configuration);
            options.UseSqlite(connectionString)
                .UseLoggerFactory(LoggerFactory.Create(builder =>
                {
                    builder.AddSerilog().SetMinimumLevel(LogLevel.Information);
                })).EnableSensitiveDataLogging();
        });
        
        services.AddTransient<InputHandler>();
        services.AddTransient<FunctionAppUpdateHandler>();
        services.AddTransient<ResizeHandler>();
        services.AddTransient<FunctionActionHandler>();
        services.AddSingleton<AnimationHandler>();
        services.AddSingleton<IAnimationProvider>(sp => sp.GetRequiredService<AnimationHandler>());
        services.AddSingleton<FunctionStateCoordinator>();
        services.AddSingleton<IUiStatusState, UiStatusState>();
        services.AddTransient<AppOrchestrator>();
        services.AddTransient<ListPanelContextFactory>();
        services.AddTransient<ListPanelFactory>();
        services.AddTransient<IAzureFunctionService, AzureFunctionService>();
        services.AddTransient<IFunctionAppManagementService, FunctionAppManagementService>();
        services.AddScoped<IAzureSubscriptionService, AzureSubscriptionService>();
        services.AddSingleton<TokenCredential, DefaultAzureCredential>();
    })
    .Build();

var mainMenuService = host.Services.GetRequiredService<AppOrchestrator>();
await host.Services.MigrateDatabaseAsync(CancellationToken.None);
await mainMenuService.StartAsync();
await host.RunAsync();