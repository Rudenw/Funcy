namespace Funcy.Core.Model;

public record FunctionDetails
{
    public required string Name { get; set; }
    public required string Trigger { get; set; }
}