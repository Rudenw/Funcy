using Funcy.Core.Model;

namespace Funcy.Console.Handlers.Models;

public record CachedFunctionAppModel(FunctionAppDetails FunctionAppDetails, DateTime LastUpdated);