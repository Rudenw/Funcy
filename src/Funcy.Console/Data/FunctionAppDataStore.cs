using Funcy.Infrastructure.Model;

namespace Funcy.Console.Data;

public class FunctionAppDataStore
{
    private readonly Lock _lock = new();
    public List<FunctionAppDetails> FunctionAppDetails { get; private set; }

    public void UpdateData(List<FunctionAppDetails> functionAppDetails)
    {
        lock (_lock)
        {
            FunctionAppDetails = functionAppDetails.ToList();
            SortFunctionAppDetails();
        }
    }
    
    private void SortFunctionAppDetails()
    {
        FunctionAppDetails.Sort((a, b) =>
        {
            var systemCompare = string.Compare(a.System, b.System, StringComparison.Ordinal);
            return systemCompare != 0 ? systemCompare : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });
    }
}