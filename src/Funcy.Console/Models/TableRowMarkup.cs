using Spectre.Console;

namespace Funcy.Console.Models;

public record TableRowMarkup
{
    public required Markup SelectedName { get; set; }
    public required Markup SelectedState { get; set; }
    public required Markup SelectedSystem { get; set; }
    
    public required Markup UnselectedName { get; set; }
    public required Markup UnselectedState { get; set; }
    public required Markup UnselectedSystem { get; set; }

    public List<Markup> Columns => [UnselectedName, UnselectedState, UnselectedSystem];
}