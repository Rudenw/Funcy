using Funcy.Console.Input;
using Funcy.Infrastructure.Azure;
using Funcy.Infrastructure.Model;

namespace Funcy.Console.Dispatching;

public class FunctionActionDispatcher
{
    private readonly AzureFunctionService _functionService;

    public FunctionActionDispatcher(AzureFunctionService functionService)
    {
        _functionService = functionService;
    }

    public void Dispatch(InputActionResult inputResult)
    {
        switch (inputResult.Action)
        {
            case FunctionAction.Start:
                _functionService.StartFunction(inputResult.FunctionAppDetails);
                break;
            case FunctionAction.Stop:
                _functionService.StopFunction(inputResult.FunctionAppDetails);
                break;
            case FunctionAction.Swap:
                _functionService.SwapFunction(inputResult.FunctionAppDetails);
                break;
        }
    }
}