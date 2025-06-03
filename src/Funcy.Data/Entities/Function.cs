namespace Funcy.Data.Entities;

public class Function
{
    public long Id { get; set; }
    public required string AzureId { get; set; }
    public required string Name { get; init; }
    public required string Trigger { get; init; }
    
    public FunctionApp? FunctionApp { get; set; }
}