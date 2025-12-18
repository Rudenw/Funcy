using System.Runtime.CompilerServices;
using Funcy.Core.Model;

namespace Funcy.Core.Interfaces;

public interface IAzureFunctionService
{
    Task<List<FunctionAppDetails>> GetFunctionsFromDatabase(CancellationToken cancellationToken);
    IAsyncEnumerable<FunctionAppFetchResult> GetFunctionAppDetailsAsync(CancellationToken cancellationToken);
    Task<FunctionAppDetails> GetFunctionAppDetails(FunctionAppDetails functionAppDetails);

    IAsyncEnumerable<FunctionAppFetchResult> GetFunctionAppFunctionsAndSlotsAsync(
        List<FunctionAppDetails> functionAppDetails, CancellationToken cancellationToken);
}