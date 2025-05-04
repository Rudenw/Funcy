namespace Funcy.Data.Entities;

public class Function
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Trigger { get; set; }
    
    public FunctionApp FunctionApp { get; set; }
}