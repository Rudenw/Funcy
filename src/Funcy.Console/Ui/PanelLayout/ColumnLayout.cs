namespace Funcy.Console.Ui.PanelLayout;

public sealed record Column(string Header);

public sealed class ColumnLayout(params Column[] columns)
{
    public IReadOnlyList<Column> Columns { get; } = columns;
}