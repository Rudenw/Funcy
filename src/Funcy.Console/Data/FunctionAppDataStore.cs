using Funcy.Core.Model;

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
    
    public void UpdatePartialData(List<FunctionAppDetails> functionAppDetails)
    {
        lock (_lock)
        {
            foreach (var functionAppDetail in functionAppDetails)
            {
                var existing = FunctionAppDetails.FirstOrDefault(x => x.Name == functionAppDetail.Name);
                if (existing is not null)
                {
                    FunctionAppDetails.Remove(existing);
                }

                FunctionAppDetails.Add(functionAppDetail);
            }
            
            SortFunctionAppDetails();
        }
    }
    
    public void RemoveFunctionApps(List<FunctionAppDetails> removed)
    {
        lock (_lock)
        {
            FunctionAppDetails.RemoveAll(x => removed.Any(y => y.Name == x.Name));
            
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