using Funcy.Console.Ui.PanelLayout;
using Funcy.Console.Ui.PanelLayout.Renderers;
using Funcy.Console.Ui.Renderers;
using Funcy.Core.Model;
using Spectre.Console;
using Spectre.Console.Rendering;
using Xunit;

namespace Funcy.Tests.Renderers;

// The generic wrap test only proves no-whitespace content crops to one line. Real log messages
// contain spaces, which Spectre word-wraps even on a NoWrap column once the text overflows the
// (padding-shrunk) Message column. These tests lock the log-specific behaviour: a long message
// stays on one line, and the empty state renders under the Message column, not the narrow Time one.
public class LogEntryRenderingTests
{
    private static string[] RenderLines(Table table, int profileWidth)
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(writer),
        });
        console.Profile.Width = profileWidth;
        console.Write(table);
        return writer.ToString()
            .Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => l.Length > 0)
            .ToArray();
    }

    private static (ListPanelTableRenderer<LogEntryDetails> Renderer, LogEntryLayoutRenderer Layout) Build(int tableWidth)
    {
        var layout = new LogEntryLayoutRenderer();
        var columnLayout = layout.CreateColumnLayout();
        var renderer = new ListPanelTableRenderer<LogEntryDetails>(columnLayout, tableWidth);

        var resolved = columnLayout.Resolve(tableWidth);
        var byHeader = new Dictionary<string, int>();
        for (var i = 0; i < columnLayout.Columns.Count; i++)
        {
            byHeader[columnLayout.Columns[i].Header] = resolved[i];
        }
        layout.SetResolvedWidths(byHeader);
        return (renderer, layout);
    }

    [Theory]
    [InlineData(115)]
    [InlineData(180)]
    public void LongWhitespaceMessage_StaysOnOneLine(int tableWidth)
    {
        var (renderer, layout) = Build(tableWidth);

        renderer.Render([], selectedIndex: -1, animatingKeys: null);
        var headerLines = RenderLines(renderer.Table, tableWidth + 20).Length;

        var entry = new LogEntryDetails
        {
            Timestamp = new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero),
            ItemType = LogItemType.Trace,
            Severity = "Info",
            Message = string.Concat(Enumerable.Repeat("Lorem ipsum dolor sit amet ", 30)),
            Key = "k1",
        };
        renderer.Render([layout.CreateRowMarkup(entry)], selectedIndex: 0, animatingKeys: null);

        var lines = RenderLines(renderer.Table, tableWidth + 20);

        Assert.Equal(headerLines + 1, lines.Length);
    }

    [Fact]
    public void EmptyState_RendersUnderMessageColumn_NotTime()
    {
        var (renderer, _) = Build(115);
        const string message = "No log entries yet.";
        renderer.RenderEmpty(message);

        var lines = RenderLines(renderer.Table, 135);
        var row = lines.Single(l => l.Contains(message));

        // The message sits under the Message header, so it is indented well past the Time/Type/Sev
        // columns rather than starting at column 0.
        var indent = row.Length - row.TrimStart().Length;
        Assert.True(indent >= 30, $"expected the empty message under Message (indent >= 30) but was {indent}");
    }
}
