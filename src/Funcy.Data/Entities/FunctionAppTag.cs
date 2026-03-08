namespace Funcy.Data.Entities;

public class FunctionAppTag
{
    public long FunctionAppId { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public FunctionApp FunctionApp { get; set; } = null!;
}
