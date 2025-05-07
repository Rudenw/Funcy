using Funcy.Data.Entities;
using Funcy.Infrastructure.Model;

namespace Funcy.Infrastructure.Mappers;

public static class FunctionAppDetailsMapper
{
    public static FunctionAppDetails Map(this FunctionApp functionApp)
    {
        return new FunctionAppDetails
        {
            Name = functionApp.Name,
            State = functionApp.State,
            System = functionApp.System,
            ResourceGroup = functionApp.ResourceGroup,
            Functions = functionApp.Functions.Select(x => x.Map()).ToList()
        };
    }
}