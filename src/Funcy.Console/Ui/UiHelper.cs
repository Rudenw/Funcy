using Spectre.Console;

namespace Funcy.Console.Ui;

public static class UiHelper
{
    public static Color GetStatusColor(string state)
    {
        return state == "Running" ? Color.Green : Color.Red;
    }
}