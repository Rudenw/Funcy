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
    public FunctionAction? Action { get; set; }
    public bool IsSwapping => Status == StatusType.InProgress && Action == FunctionAction.Swap;
    public string ToDisplayLabel()
    {
        return Status switch
        {
            StatusType.Idle => "",
            StatusType.Success => "",
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