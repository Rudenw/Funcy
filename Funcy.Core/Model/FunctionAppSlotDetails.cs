namespace Funcy.Core.Model;

public class FunctionAppSlotDetails : IComparable<FunctionAppSlotDetails>, IHasKey
{
    public required string Id { get; set; }
    public required string FullName { get; set; }
    public required string Name { get; set; }
    public required FunctionState State { get; set; }
    public FunctionStatus Status { get; set; } = new();
    public int CompareTo(FunctionAppSlotDetails? other)
    {
        if (other is null)
        {
            return 1;
        }
        
        return StringComparer.Ordinal.Compare(Name, other.Name);
    }

    public string Key => FullName;
}