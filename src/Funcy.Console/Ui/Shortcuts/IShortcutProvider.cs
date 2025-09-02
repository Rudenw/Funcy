using Funcy.Core.Model;

namespace Funcy.Console.Ui.Shortcuts;

public interface IShortcutProvider<T> where T : IHasKey, IComparable<T>
{
    List<ShortcutMap> Describe(T? item);
}