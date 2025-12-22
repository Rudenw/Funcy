using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui;

public static class UiHelper
{
    public static Color GetStateColor(FunctionState state)
    {
        return state switch
        {
            FunctionState.Running => Color.Green,
            FunctionState.Stopped => Color.Red,
            _ => Color.White
        };
    }
    
    public static Color GetStatusColor(FunctionStatus status)
    {
        if (status.Status == StatusType.Swapped)
        {
            return Color.CornflowerBlue;
        }
        
        return status.Status switch
        {
            StatusType.Success => Color.Green,
            StatusType.Error => Color.Red,
            _ => Color.White
        };
    }
    
    
}