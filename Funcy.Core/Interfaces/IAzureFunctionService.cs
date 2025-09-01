using System.Runtime.CompilerServices;
using Funcy.Core.Model;

namespace Funcy.Core.Interfaces;

public interface IAzureFunctionService
{
    IAsyncEnumerable<FunctionAppFetchResult> FetchFunctionAppDetailsAsync(CancellationToken cancellationToken);
    Task<List<FunctionAppDetails>> GetFunctionsFromDatabase(CancellationToken cancellationToken);

    IAsyncEnumerable<FunctionAppFetchResult> FetchSpecificFunctionAppDetailsAsync(
        IEnumerable<FunctionAppDetails> functionAppDetails, CancellationToken cancellationToken);

    Task RemoveFunctionsFromDatabase(IEnumerable<FunctionAppDetails> removedFunctionApps);
}