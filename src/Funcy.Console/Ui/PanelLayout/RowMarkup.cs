using Spectre.Console;

namespace Funcy.Console.Ui.PanelLayout;

public sealed record RowCell(Markup Selected, Markup Unselected)
{
    public Markup Get(bool isSelected) => isSelected ? Selected : Unselected;
}

public sealed class RowMarkup
{
    public required string Key { get; init; }
    public Dictionary<string, RowCell> Cells { get; } = new(StringComparer.Ordinal);

    public RowMarkup Add(string columnKey, RowCell cell)
    {
        Cells[columnKey] = cell;
        return this;
    }

    public Markup GetCell(string columnKey, bool isSelected)
        => Cells.TryGetValue(columnKey, out var cell) ? cell.Get(isSelected) : new Markup(" ");
}