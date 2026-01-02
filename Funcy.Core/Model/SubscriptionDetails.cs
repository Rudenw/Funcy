namespace Funcy.Core.Model;

public class SubscriptionDetails : IComparable<SubscriptionDetails>, IHasKey
{
    public required string Name { get; init; }
    public bool Current { get; set; }
    public string Key => Name;
    public List<FunctionAppDetails> FunctionAppDetails { get; set; } = [];
    public required string Id { get; init; }
    
    public int CompareTo(SubscriptionDetails? other)
    {
        return other is null ? 1 : StringComparer.Ordinal.Compare(Name, other.Name);
    }
}