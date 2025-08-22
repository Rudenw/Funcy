namespace Funcy.Core.Model;

public enum FunctionAction
{
    Start,
    Stop,
    Swap,
}

public static class FunctionActionExtensions
{
    public static TransientState GetTransientState(this FunctionAction action)
    {
        return action switch
        {
            FunctionAction.Start => TransientState.Starting,
            FunctionAction.Stop => TransientState.Stopping,
            FunctionAction.Swap => TransientState.Swapping,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
    
    public static RealState GetRealState(this FunctionAction action)
    {
        return action switch
        {
            FunctionAction.Start => RealState.Running,
            FunctionAction.Stop => RealState.Stopped,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}