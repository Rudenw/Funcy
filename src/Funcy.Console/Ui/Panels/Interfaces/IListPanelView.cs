using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.Panels.GenericTestPanel;

public interface IListPanelView<in T> : IListPanel where T : IComparable<T>, IHasKey
{
    void SetItems(IReadOnlyList<T> items);
}