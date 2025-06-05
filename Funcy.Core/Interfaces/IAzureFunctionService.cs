using System.Runtime.CompilerServices;
using Funcy.Core.Model;

namespace Funcy.Core.Interfaces;

public interface IAzureFunctionService
{
    IAsyncEnumerable<FunctionAppFetchResult> FetchFunctionAppDetailsAsync(CancellationToken cancellationToken);
    List<FunctionAppDetails> GetFunctionsFromDatabase();

    IAsyncEnumerable<FunctionAppFetchResult> FetchSpecificFunctionAppDetailsAsync(
        IEnumerable<FunctionAppDetails> functionAppDetails, CancellationToken cancellationToken);

    Task RemoveFunctionsFromDatabase(IEnumerable<FunctionAppDetails> removedFunctionApps);
}