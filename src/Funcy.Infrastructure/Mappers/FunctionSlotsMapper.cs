using Funcy.Core.Model;
using Funcy.Data.Entities;

namespace Funcy.Infrastructure.Mappers;

public static class FunctionSlotsMapper
{
    public static FunctionAppSlotDetails Map(this FunctionAppSlot slot)
    {
        return new FunctionAppSlotDetails
        {
            Id = slot.AzureId,
            FullName = slot.FullName,
            Name = slot.Name,
            State = slot.State
        };
    }
}