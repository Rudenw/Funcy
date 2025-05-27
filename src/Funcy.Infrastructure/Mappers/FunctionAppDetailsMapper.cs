using Funcy.Core.Model;
using Funcy.Data.Entities;

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
            Subscription = functionApp.Subscription,
            Functions = functionApp.Functions.Select(x => x.Map()).ToList()
        };
    }

    public static FunctionApp MapToEntity(this FunctionAppDetails details)
    {
        return new FunctionApp
        {
            Name = details.Name,
            State = details.State,
            System = details.System,
            ResourceGroup = details.ResourceGroup,
            Subscription = details.Subscription,
            Functions = details.Functions.Select(x => x.MapToEntity()).ToList()
        };
    }
}