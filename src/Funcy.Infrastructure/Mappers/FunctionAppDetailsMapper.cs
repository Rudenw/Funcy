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
            State = new FunctionState { RealState = functionApp.State },
            System = functionApp.System,
            ResourceGroup = functionApp.ResourceGroup,
            Subscription = functionApp.Subscription,
            Functions = functionApp.Functions.Select(x => x.Map()).ToList(),
            Slots = functionApp.Slots.Select(x => x.Map()).ToList()
        };
    }
}