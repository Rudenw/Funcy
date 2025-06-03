using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.Factories.Models;

public record TableRowMarkup
{
    public required Markup SelectedName { get; set; }
    public required Markup SelectedState { get; set; }
    public required Markup SelectedSystem { get; set; }
    
    public required Markup UnselectedName { get; set; }
    public required Markup UnselectedState { get; set; }
    public required Markup UnselectedSystem { get; set; }
    public bool CanExpand { get; set; }
    public required Markup SelectedExpanded { get; set; }
    public required Markup SelectedUnexpanded { get; set; }
    public required Markup UnselectedExpanded { get; set; }
    public required Markup UnselectedUnexpanded { get; set; }
    
    public Markup GetName(bool isSelected) => isSelected ? SelectedName : UnselectedName;
    public Markup GetState(bool isSelected) => isSelected ? SelectedState : UnselectedState;
    public Markup GetSystem(bool isSelected) => isSelected ? SelectedSystem : UnselectedSystem;
    public Markup GetExpanded(bool isSelected) => isSelected ? SelectedExpanded : UnselectedExpanded;
    public Markup GetUnexpanded(bool isSelected) => isSelected ? SelectedUnexpanded : UnselectedUnexpanded;
    
    public required FunctionAppDetails FunctionAppDetails { get; set; }
}