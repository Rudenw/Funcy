namespace Funcy.Core.Model;

public record FunctionAppDetails
{
    public required string Name { get; set; }
    public required string State { get; set; }
    public required string System { get; set; }
    public List<FunctionDetails> Functions { get; set; } = [];
    public required string ResourceGroup { get; set; }
    public required string Subscription { get; set; }
    
    public string Id => $"/subscriptions/{Subscription}/resourceGroups/{ResourceGroup}/providers/Microsoft.Web/sites/{Name}";
}