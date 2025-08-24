namespace Funcy.Console.Ui.PanelLayout;

public sealed record Column(string Header);

public sealed class ColumnLayout
{
    public IReadOnlyList<Column> Columns { get; }
    public ColumnLayout(params Column[] columns) => Columns = columns;
}