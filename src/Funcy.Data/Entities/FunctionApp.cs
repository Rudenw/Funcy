using System.ComponentModel.DataAnnotations;

namespace Funcy.Console.Data.Entities;

public class FunctionApp
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string System { get; set; }
    public List<Function> Functions { get; } = [];
    public string ResourceGroup { get; set; }
}