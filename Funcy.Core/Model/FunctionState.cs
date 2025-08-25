namespace Funcy.Core.Model;

public enum FunctionState
{
    Unknown,
    Running,
    Stopped
}

public static class FunctionStateExtensions
{
    public static string ToDisplayLabel(this FunctionState state)
    {
        return state switch
        {
            FunctionState.Running => "Running",
            FunctionState.Stopped => "Stopped",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }
}