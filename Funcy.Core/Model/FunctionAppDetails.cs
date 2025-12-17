namespace Funcy.Core.Model;

public class FunctionAppDetails : IComparable<FunctionAppDetails>, IHasKey
{
    public required string Name { get; init; }
    public string Key => Name;
    public required FunctionState State { get; set; }
    public FunctionStatus Status { get; set; } = new();
    public required string System { get; init; }
    public List<FunctionAppSlotDetails> Slots { get; set; } = [];
    public List<FunctionDetails> Functions { get; set; } = [];
    public string AnimatingFrame { get; set; } = "";
    public DateTime LastUpdated { get; set; }
    public bool DetailsLoaded { get; set; } = false;

    public List<FunctionAppSlotDetails> SlotsExtra =>
        [new() { FullName = $"{Name} (Production)", Name = "Production", State = State, Id = ""}, ..Slots];
    
    public required string Id { get; init; }


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