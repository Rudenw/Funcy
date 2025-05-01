using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Funcy.Console;
using Funcy.Console.Data;
using Funcy.Console.Ui;
using Funcy.Console.Ui.Triggers;
using Funcy.Infrastructure.Azure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

const string subscriptionId = "ee691e14-38ba-4613-91bc-2287244a60e7";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<FunctionAppDbContext>(options =>
            options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection")));
        services.AddTransient<InputHandler>();
        services.AddTransient<TableRowUpdater>();
        services.AddTransient<ResizeHandler>();
        services.AddTransient<MainMenuService>();
        services.AddTransient<AzureFunctionService>(_ => new AzureFunctionService(subscriptionId));
    })
    .Build();

// Hämta vår service från DI-containern
var helloService = host.Services.GetRequiredService<MainMenuService>();
await helloService.StartAsync();

// Håll hosten igång tills användaren avslutar
await host.RunAsync();