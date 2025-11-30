namespace Funcy.Core.Model;

public class FunctionAppDetails : IComparable<FunctionAppDetails>, IHasKey
{
    public required string Name { get; init; }
    public string Key => Name;
    public required FunctionState State { get; set; }
    public FunctionStatus Status { get; set; } = new();
    public required string System { get; init; }
    public List<FunctionAppSlotDetails> Slots { get; init; } = [];
    public List<FunctionDetails> Functions { get; init; } = [];
    public required string ResourceGroup { get; init; }
    public required string Subscription { get; init; }
    public DateTime LastUpdated { get; set; }

    public List<FunctionAppSlotDetails> SlotsExtra =>
        [new() { FullName = $"{Name} (Production)", Name = "Production", State = State, Id = ""}, ..Slots];
    
    public string Id => $"/subscriptions/{Subscription}/resourceGroups/{ResourceGroup}/providers/Microsoft.Web/sites/{Name}";


    public int CompareTo(FunctionAppDetails? other)
    {
        if (other is null)
        {
            return 1;
        }
        
        var bySystem = StringComparer.Ordinal.Compare(System, other.System);
        return bySystem != 0 ? bySystem : StringComparer.Ordinal.Compare(Name, other.Name);
    }
}