using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.PanelLayout.Renderers;

public class AppSettingLayoutRenderer : ILayoutRenderer<AppSettingDetails>
{
    public RowMarkup CreateRowMarkup(AppSettingDetails item)
    {
        var rowMarkup = new RowMarkup { Key = item.Key };

        var name = Markup.Escape(item.Name);
        rowMarkup.Add("Name", new RowCell(UiStyles.CreateSelectedCell(name), new Markup(name)));

        var value = AppSettingValueFormatter.Format(item);
        rowMarkup.Add("Value", new RowCell(UiStyles.CreateSelectedCell(value.Selected), new Markup(value.Unselected)));

        var source = FormatSource(item);
        rowMarkup.Add("Source", new RowCell(UiStyles.CreateSelectedCell(source.Selected), new Markup(source.Unselected)));

        return rowMarkup;
    }

    public ColumnLayout<AppSettingDetails> CreateColumnLayout()
    {
        // Only Name is sortable; the natural order is already by name. Value/Source have no
        // selector so filtering/sorting can never key off a (possibly hidden) value.
        // Name and Value flex so the spare width on a wide terminal is used instead of left blank;
        // Source is fixed just wide enough for "Key Vault (<vault-name>)" (~30 chars).
        return new ColumnLayout<AppSettingDetails>(
            new Column<AppSettingDetails>("Name", f => f.Name, 40, Flex: true),
            new Column<AppSettingDetails>("Value", null, 43, Flex: true),
            new Column<AppSettingDetails>("Source", null, 32));
    }

    private static (string Unselected, string Selected) FormatSource(AppSettingDetails item)
    {
        if (item.KeyVaultReference is null)
        {
            return (string.Empty, string.Empty);
        }

        var label = $"Key Vault ({item.KeyVaultReference.VaultName})";
        var escaped = Markup.Escape(label);
        return ($"[{UiStyles.Hint}]{escaped}[/]", escaped);
    }
}
