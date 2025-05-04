namespace Funcy.Infrastructure.Model;

public record FunctionAppDetails
{
    public required string Name { get; set; }
    public required string State { get; set; }
    public required string System { get; set; }
    public List<FunctionDetails> Functions { get; set; } = [];
    public required string ResourceGroup { get; set; }
}