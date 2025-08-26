namespace Funcy.Core.Model;

public class FunctionDetails : IComparable<FunctionDetails>, IHasKey
{
    public required string FunctionAppName { get; set; }
    public required string Name { get; set; }
    public required string Trigger { get; set; }
    public int CompareTo(FunctionDetails? other)
    {
        if (other is null)
        {
            return 1;
        }
        
        var byFunctionApp = StringComparer.Ordinal.Compare(FunctionAppName, other.FunctionAppName);
        return byFunctionApp != 0 ? byFunctionApp : StringComparer.Ordinal.Compare(Name, other.Name);
    }

    public string Key => FunctionAppName + Name;
}