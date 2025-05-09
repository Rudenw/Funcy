using Funcy.Infrastructure.Model;

namespace Funcy.Console.Data;

public class FunctionAppDataStore
{
    private readonly Lock _lock = new();
    public List<FunctionAppDetails> FunctionAppDetails { get; private set; }
    public event Action<List<FunctionAppDetails>>? OnDataChanged;

    public FunctionAppDataStore(List<FunctionAppDetails> functionAppDetails)
    {
        lock (_lock)
        {
            FunctionAppDetails = functionAppDetails.ToList();
            SortFunctionAppDetails();
        }
    }

    public void Update(List<FunctionAppDetails> functionAppDetails)
    {
        lock (_lock)
        {
            FunctionAppDetails = functionAppDetails.ToList();
            SortFunctionAppDetails();
        }
        
        OnDataChanged?.Invoke(FunctionAppDetails);
    }
    
    private void SortFunctionAppDetails()
    {
        lock (_lock)
        {
            FunctionAppDetails.Sort((a, b) =>
            {
                var systemCompare = string.Compare(a.System, b.System, StringComparison.Ordinal);
                return systemCompare != 0 ? systemCompare : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });
        }
    }
}