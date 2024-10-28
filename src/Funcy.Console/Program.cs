using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Funcy.Console;
using Funcy.Infrastructure.Azure;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Registrera IHelloService och dess implementation HelloService
        services.AddTransient<MainMenuService>();
        services.AddTransient<AzureFunctionService>();
    })
    .Build();

// Hämta vår service från DI-containern
var helloService = host.Services.GetRequiredService<MainMenuService>();
helloService.ShowMainMenuAsync();

// Håll hosten igång tills användaren avslutar
await host.RunAsync();