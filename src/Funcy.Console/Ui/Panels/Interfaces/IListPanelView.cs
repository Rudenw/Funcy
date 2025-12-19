using Funcy.Core.Model;

namespace Funcy.Console.Ui.Panels.Interfaces;

public interface IListPanelView<T> : IListPanel where T : IComparable<T>, IHasKey
{
    void SetItems(IReadOnlyList<T> items);
    void SetUiStatus(UiStatusSnapshot uiStatusSnapshot);
}