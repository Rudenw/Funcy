using Funcy.Console.Ui.Shortcuts;
using Funcy.Console.Ui.State;
using Funcy.Core.Model;
using Xunit;

namespace Funcy.Tests.Ui;

public class FunctionAppShortcutProviderTests
{
    private static FunctionAppDetails CreateApp() => new()
    {
        Name = "my-app",
        State = FunctionState.Running,
        ResourceGroup = "rg",
        Subscription = "sub",
        Id = "id"
    };

    [Fact]
    public void Describe_WhenNotRefreshing_EnablesRefreshAllShortcut()
    {
        var uiStatus = new UiStatusState();
        var sut = new FunctionAppShortcutProvider(uiStatus);

        var shortcuts = sut.Describe(CreateApp());

        Assert.True(shortcuts[new TableIndex(1, 4)].IsEnabled);
    }

    [Fact]
    public void Describe_WhenRefreshing_DisablesRefreshAllShortcut()
    {
        var uiStatus = new UiStatusState();
        uiStatus.BeginInventoryValidation();
        var sut = new FunctionAppShortcutProvider(uiStatus);

        var shortcuts = sut.Describe(CreateApp());

        Assert.False(shortcuts[new TableIndex(1, 4)].IsEnabled);
    }

    [Fact]
    public void IsActionValid_WhenRefreshing_ReturnsFalseForRefreshAll()
    {
        var uiStatus = new UiStatusState();
        uiStatus.BeginDetailsRefresh();
        var sut = new FunctionAppShortcutProvider(uiStatus);

        var isValid = sut.IsActionValid(CreateApp(), FunctionAction.RefreshAll);

        Assert.False(isValid);
    }
}
