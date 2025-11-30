namespace Funcy.Console.Ui.PanelLayout.Renderers;

public interface ILayoutRenderer<T>
{
    RowMarkup CreateRowMarkup(T item);
    ColumnLayout<T> CreateColumnLayout();
}