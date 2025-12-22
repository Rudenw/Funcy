namespace Funcy.Core.Model;

public enum StatusType
{
    Idle,
    InProgress,
    Success,
    Error,
    Swapped
}

public class FunctionStatus
{
    public StatusType Status { get; set; }
    public FunctionAction? Action { get; set; }

    public int GetTimeToLive()
    {
        return Status switch
        {
            StatusType.InProgress => 50,
            StatusType.Success => 3,
            StatusType.Error => 0,
            StatusType.Swapped => 60,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public string ToDisplayLabel()
    {
        return Status switch
        {
            StatusType.Idle => "",
            StatusType.Success => "Success",
            StatusType.Error => "Error",
            StatusType.Swapped => "Swapped",
            StatusType.InProgress => Action switch
            {
                FunctionAction.Start => "Starting...",
                FunctionAction.Stop => "Stopping...",
                FunctionAction.Swap => "Swapping...",
                FunctionAction.Refresh => "Refreshing...",
                _ => ""
            },
            _ => ""
        };
    }
}