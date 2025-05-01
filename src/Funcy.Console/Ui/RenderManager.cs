using Funcy.Console.Ui.Panels;

namespace Funcy.Console.Ui;

public class RenderManager
{
    public void Render(FunctionAppPanel panel, InputHandler input)
    {
        // Använd panel-metoderna och input-värden för att uppdatera state
    }

    public void OnResize(FunctionAppPanel panel)
    {
        //panel.UpdateMaxVisibleRows();
        panel.CreateFunctionAppPanel();
    }
}