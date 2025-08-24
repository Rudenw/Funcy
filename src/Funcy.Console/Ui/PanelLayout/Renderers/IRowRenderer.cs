namespace Funcy.Console.Ui.PanelLayout.Renderers;

public interface ILayoutRenderer<in T>
{
    RowMarkup CreateRowMarkup(T item);
    ColumnLayout CreateColumnLayout();
}