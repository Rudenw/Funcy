using Funcy.Core.Model;

namespace Funcy.Console.Ui.Shortcuts;

public interface IShortcutProvider<T> where T : IHasKey, IComparable<T>
{
    Dictionary<TableIndex, ShortcutMap> Describe(T? item);
}