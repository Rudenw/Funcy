using Funcy.Data.Entities;
using Funcy.Infrastructure.Model;

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
}