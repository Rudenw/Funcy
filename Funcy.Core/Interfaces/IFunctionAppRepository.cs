using Funcy.Core.Model;

namespace Funcy.Core.Interfaces;

public interface IFunctionAppRepository
{
    Task<FunctionAppDetails> UpsertAsync(FunctionAppDetails details, List<FunctionDetails>? functions, CancellationToken cancellationToken);
    Task RemoveAsync(FunctionAppDetails details, CancellationToken cancellationToken);
    Task<List<FunctionAppDetails>> GetAllAsync(CancellationToken cancellationToken);
}
