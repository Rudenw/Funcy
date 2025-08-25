using Funcy.Core.Model;

namespace Funcy.Console.Handlers.Models;

public record FunctionAppUpdate
{
    public required UpdateSource Source { get; set; }
    public required FunctionAppDetails FunctionAppDetails { get; set; }
}