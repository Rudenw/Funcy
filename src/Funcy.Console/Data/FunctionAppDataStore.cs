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
            var needsSorting = false;
            foreach (var functionAppDetail in functionAppDetails)
            {
                var findIndex = FunctionAppDetails.FindIndex(x => x.Name == functionAppDetail.Name);
                if (findIndex >= 0)
                {
                    FunctionAppDetails[findIndex] = functionAppDetail;
                }
                else
                {
                    needsSorting = true;
                    FunctionAppDetails.Add(functionAppDetail);
                }
            }

            if (needsSorting)
            {
                SortFunctionAppDetails();                
            }
        }
    }
    
    public void RemoveFunctionApps(List<FunctionAppDetails> removed)
    {
        lock (_lock)
        {
            FunctionAppDetails.RemoveAll(x => removed.Any(y => y.Name == x.Name));
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