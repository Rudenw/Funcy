using System.Runtime.CompilerServices;
using Funcy.Core.Model;

namespace Funcy.Core.Interfaces;

public interface IAzureFunctionService
{
    Task<IEnumerable<FunctionAppDetails>> GetFunctionAppBaseAsync();
    IAsyncEnumerable<FunctionAppFetchResult> GetFunctionAppWithAllDataAsync(CancellationToken cancellationToken);
    Task<FunctionAppDetails> GetFunctionAppDetails(FunctionAppDetails functionAppDetails);
}