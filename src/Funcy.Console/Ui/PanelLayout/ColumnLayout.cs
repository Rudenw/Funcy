namespace Funcy.Console.Ui.PanelLayout;

public sealed record Column(string Header, int Width = 0);

public sealed class ColumnLayout(params Column[] columns)
{
    public IReadOnlyList<Column> Columns { get; } = columns;
}