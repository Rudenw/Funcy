using Funcy.Console.Ui.Input;
using Funcy.Core.Interfaces;

namespace Funcy.Console.Dispatching;

public class FunctionActionDispatcher(IFunctionAppManagementService functionAppManagement)
{
    public void Dispatch(InputActionResult inputResult)
    {
        switch (inputResult.Action)
        {
            case FunctionAction.Start:
                functionAppManagement.StartFunction(inputResult.FunctionAppDetails);
                break;
            case FunctionAction.Stop:
                functionAppManagement.StopFunction(inputResult.FunctionAppDetails);
                break;
            case FunctionAction.Swap:
                functionAppManagement.SwapFunction(inputResult.FunctionAppDetails);
                break;
        }
    }
}