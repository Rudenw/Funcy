namespace Funcy.Core.Model;

public enum RealState { Unknown, Running, Stopped }
public enum TransientState { None, Starting, Stopping, Swapping }

public sealed class FunctionState()
{
    public RealState RealState { get; set; }
    public TransientState TransientState { get; set; }
    public bool HasTransientState => TransientState != TransientState.None;

    public bool CanStart()
    {
        return !HasTransientState && RealState == RealState.Stopped;
    }
    
    public bool CanStop()
    {
        return !HasTransientState && RealState == RealState.Running;
    }
    
    public bool CanSwap()
    {
        return !HasTransientState;
    }
    
    public string ToDisplayLabel()
    {
        return TransientState switch
        {
            TransientState.Starting => "Starting...",
            TransientState.Stopping => "Stopping...",
            TransientState.Swapping => "Swapping...",
            _ => RealState switch
            {
                RealState.Running => "Running",
                RealState.Stopped => "Stopped",
                _ => "Unknown"
            }
        };
    }
}