namespace Funcy.Data.Entities;

public class FunctionApp
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string System { get; set; }
    public List<Function> Functions { get; set; } = [];
    public string ResourceGroup { get; set; }
    public string Subscription { get; set; }
    public string State { get; set; }
}