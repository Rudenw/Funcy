using Funcy.Console.Ui.Panels;

namespace Funcy.Console.Ui.Navigation;

public sealed record NavigationRequest(PanelTarget Target, string Key);