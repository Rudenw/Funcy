namespace Funcy.Core.Model;

public class FunctionAppSlotDetails
{
    public required string Id { get; set; }
    public required string FullName { get; set; }
    public required string Name { get; set; }
    public required FunctionState State { get; set; }
}