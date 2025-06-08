namespace Funcy.Data.Entities;

public class FunctionAppSlot
{
    public long Id { get; set; }
    public required string FullName { get; set; }
    public required string Name { get; set; }
    public required string AzureId { get; set; }
    public required string State { get; set; }
    public long FunctionAppId { get; set; }
    public FunctionApp? FunctionApp { get; set; }
}