namespace Funcy.Infrastructure.Model;

public record FunctionAppDetails
{
    public string Name { get; set; }
    public string State { get; set; }
    public string System { get; set; }
    public List<FunctionDetails> Functions { get; set; }
}