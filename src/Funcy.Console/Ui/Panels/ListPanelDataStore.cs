using Funcy.Core.Model;

namespace Funcy.Console.Ui.Panels;

public class ListPanelDataStore<T> where T : IComparable<T>, IHasKey
{
    private readonly Lock _gate = new();

    private Dictionary<string, T> _items = new();
    private List<T> _sorted = [];
    private bool _dirty = true;
    
    public void UpdateAll(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        
        lock (_gate)
        {
            _items = new Dictionary<string, T>();
            foreach (var item in items)
            {
                _items[item.Key] = item;
            }

            _dirty = true;
        }
    }
    
    public void UpsertMany(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        lock (_gate)
        {
            var anyChange = false;

            foreach (var incoming in items)
            {
                _items[incoming.Key] = incoming;
                anyChange = true;
            }

            if (anyChange)
            {
                _dirty = true;
            }
        }
    }
    
    public void RemoveByKeys(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        lock (_gate)
        {
            var removed = false;
            foreach (var k in keys)
            {
                removed |= _items.Remove(k);
            }

            if (removed)
            {
                _dirty = true;
            }
        }
    }
    
    public void RemoveMany(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var keys = items.Select(key => key.Key).Distinct();
        RemoveByKeys(keys);
    }
    
    public IReadOnlyList<T> Snapshot()
    {
        lock (_gate)
        {
            if (_dirty)
            {
                _sorted = _items.Values.ToList();
                _sorted.Sort();
                _dirty = false;
            }
            
            return _sorted.ToArray();
        }
    }
}