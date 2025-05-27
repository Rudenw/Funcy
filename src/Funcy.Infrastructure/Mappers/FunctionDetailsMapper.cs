using Funcy.Core.Model;
using Funcy.Data.Entities;

namespace Funcy.Infrastructure.Mappers;

public static class FunctionDetailsMapper
{
    public static FunctionDetails Map(this Function function)
    {
        return new FunctionDetails
        {
            Name = function.Name,
            Trigger = function.Trigger
        };
    }

    public static Function MapToEntity(this FunctionDetails details)
    {
        return new Function
        {
            Name = details.Name,
            Trigger = details.Trigger
        };
    }
}