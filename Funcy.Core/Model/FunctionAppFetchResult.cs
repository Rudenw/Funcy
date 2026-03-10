namespace Funcy.Core.Model;

public sealed record FunctionAppFetchResult(
    string Name,
    FunctionAppDetails? Details,
    FunctionAppUpdateKind UpdateKind,
    string? ErrorMessage = null)
{
    public bool IsSuccess => Details is not null;
}
