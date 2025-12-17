using Funcy.Core.Model;
using Funcy.Infrastructure.Azure.Models;

namespace Funcy.Infrastructure.Mappers;

public static class FunctionAppDetailsMapper
{
    public static FunctionAppDetails Map(this FunctionAppGraphRow functionApp)
    {
        return new FunctionAppDetails
        {
            Name = functionApp.Name,
            State = Enum.Parse<FunctionState>(functionApp.State),
            System = functionApp.System,
            Id = functionApp.Id,
            LastUpdated = DateTime.UtcNow
        };
    }
}