namespace Funcy.Infrastructure.Model;

public record FunctionDetails
{
    public required string Name { get; set; }
    public required string Trigger { get; set; }
    public required string Status { get; set; }
}