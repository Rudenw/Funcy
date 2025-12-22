namespace Funcy.Core.Model;

public class SubscriptionDetails : IComparable<SubscriptionDetails>, IHasKey
{
    public required string Name { get; init; }
    public required bool Current { get; init; }
    public string Key => Name;
    public List<FunctionAppDetails> FunctionAppDetails { get; set; } = [];
    public required string Id { get; set; }

    public int CompareTo(SubscriptionDetails? other)
    {
        return other is null ? 1 : StringComparer.Ordinal.Compare(Name, other.Name);
    }
}