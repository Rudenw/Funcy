namespace Funcy.Core.Model;

public enum StatusType
{
    Idle,
    InProgress,
    Success,
    Error
}

public class FunctionStatus
{
    public StatusType Status { get; set; }
    public bool HasBeenSwapped => Status == StatusType.Success && _previousAction == FunctionAction.Swap;

    public FunctionAction? Action
    {
        get => _action;
        set
        {
            _previousAction = _action;
            _action = value;
        }
    }

    private FunctionAction? _previousAction;
    private FunctionAction? _action;
    public bool IsSwapping => Status == StatusType.InProgress && Action == FunctionAction.Swap;

    public int GetTimeToLive()
    {
        if (HasBeenSwapped)
        {
            return 60;
        }
        
        return Status switch
        {
            StatusType.InProgress => 50,
            StatusType.Success => 3,
            StatusType.Error => 0,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public string ToDisplayLabel()
    {
        if (HasBeenSwapped)
        {
            return "Swapped";
        }
        
        return Status switch
        {
            StatusType.Idle => "",
            StatusType.Success => "Success",
            StatusType.Error => "Error",
            _ => Action switch
            {
                FunctionAction.Start => "Starting...",
                FunctionAction.Stop => "Stopping...",
                FunctionAction.Swap => "Swapping...",
                _ => ""
            }
        };
    }
}