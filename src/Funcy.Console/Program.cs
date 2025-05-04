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

const string subscriptionId = "ee691e14-38ba-4613-91bc-2287244a60e7";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMemoryCache();
        services.AddDbContextFactory<FunctionAppDbContext>(options =>
            options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection")));
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