using Funcy.Core.Model;
using Funcy.Data.Entities;
using Funcy.Infrastructure.Azure.Models;

namespace Funcy.Infrastructure.Mappers;

public static class FunctionAppDetailsMapper
{
    public static FunctionAppDetails Map(this FunctionAppGraphRow functionApp)
    {
        return new FunctionAppDetails
        {
            Name = functionApp.Name,
            Subscription = functionApp.SubscriptionId,
            ResourceGroup = functionApp.ResourceGroup,
            State = Enum.Parse<FunctionState>(functionApp.State),
            System = functionApp.System,
            Id = functionApp.Id,
            LastUpdated = DateTime.UtcNow
        };
    }
    
    public static FunctionAppDetails Map(this FunctionApp functionApp)
    {
        return new FunctionAppDetails
        {
            Id = functionApp.AzureId,
            Name = functionApp.Name,
            State = functionApp.State,
            Subscription = functionApp.Subscription,
            ResourceGroup = functionApp.ResourceGroup,
            System = functionApp.System,
            Functions = functionApp.Functions.Select(x => x.Map()).ToList(),
            Slots = functionApp.Slots.Select(x => x.Map()).ToList(),
            LastUpdated = functionApp.UpdatedAt
        };
    }
}