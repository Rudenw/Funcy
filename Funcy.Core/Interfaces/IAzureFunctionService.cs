using System.Runtime.CompilerServices;
using Funcy.Core.Model;

namespace Funcy.Core.Interfaces;

public interface IAzureFunctionService
{
    Task<List<FunctionAppDetails>> GetFunctionsFromDatabase(string subscriptionId);
    IAsyncEnumerable<FunctionAppFetchResult> GetFunctionAppDetailsAsync(string subscriptionId, CancellationToken cancellationToken);
    Task<FunctionAppDetails> GetFunctionAppDetails(FunctionAppDetails functionAppDetails);

    IAsyncEnumerable<FunctionAppFetchResult> GetFunctionAppFunctionsAndSlotsAsync(
        List<FunctionAppDetails> functionAppDetails, CancellationToken cancellationToken);

    Task SetPinnedAsync(string azureId, bool isPinned);

    // Persists resolved Service Bus namespace ids for an app's functions so the lookup is done once.
    Task SaveServiceBusNamespacesAsync(string functionAppArmId, IReadOnlyList<(string FunctionName, string NamespaceId)> resolved);
}