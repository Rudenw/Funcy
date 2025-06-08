namespace Funcy.Core.Model;

public class FunctionAppDetails
{
    public required string Name { get; init; }
    public required string State { get; set; }
    public required string System { get; init; }
    public List<FunctionAppSlotDetails> Slots { get; init; } = [];
    public List<FunctionDetails> Functions { get; init; } = [];
    public required string ResourceGroup { get; init; }
    public required string Subscription { get; init; }

    public List<FunctionAppSlotDetails> SlotsExtra =>
        [new() { Name = $"{Name} (Production)", State = State }, ..Slots];
    
    public string Id => $"/subscriptions/{Subscription}/resourceGroups/{ResourceGroup}/providers/Microsoft.Web/sites/{Name}";
}