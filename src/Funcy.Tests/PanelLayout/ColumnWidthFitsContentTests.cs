using Funcy.Console.Ui.PanelLayout;
using Funcy.Console.Ui.PanelLayout.Renderers;
using Funcy.Core.Model;
using Xunit;

namespace Funcy.Tests.PanelLayout;

// Bug A: fixed (non-flex) columns must be at least as wide as the widest content they can ever
// hold, so realistic values are never truncated and flex columns absorb the genuine spare width.
// Enum-backed labels are enumerated here so the assertions track the model, not a hard-coded guess.
public class ColumnWidthFitsContentTests
{
    private static int WidthOf<T>(ColumnLayout<T> layout, string header) =>
        layout.Columns.Single(c => c.Header == header).Width;

    // Every FunctionState label (Unknown has no display label and never reaches a cell).
    private static readonly string[] StateLabels =
        [FunctionState.Running.ToDisplayLabel(), FunctionState.Stopped.ToDisplayLabel()];

    // Every FunctionStatus label: the terminal states plus each in-progress action label.
    private static IEnumerable<string> StatusLabels()
    {
        foreach (var status in Enum.GetValues<StatusType>())
        {
            if (status == StatusType.InProgress)
            {
                foreach (var action in Enum.GetValues<FunctionAction>())
                {
                    yield return new FunctionStatus { Status = status, Action = action }.ToDisplayLabel();
                }
            }
            else
            {
                yield return new FunctionStatus { Status = status }.ToDisplayLabel();
            }
        }
    }

    [Fact]
    public void FunctionApps_StatusColumn_FitsWidestStatusLabel()
    {
        var layout = new FunctionAppLayoutRenderer([], _ => 20).CreateColumnLayout();
        var widest = StatusLabels().Max(l => l.Length);

        Assert.True(WidthOf(layout, "Status") >= widest,
            $"Status column {WidthOf(layout, "Status")} < widest label {widest}");
    }

    [Fact]
    public void FunctionApps_StateColumn_FitsWidestStateLabel()
    {
        var layout = new FunctionAppLayoutRenderer([], _ => 20).CreateColumnLayout();
        var widest = StateLabels.Max(l => l.Length);

        Assert.True(WidthOf(layout, "State") >= widest);
    }

    [Fact]
    public void FunctionApps_CountColumns_FitLargeCounts()
    {
        var layout = new FunctionAppLayoutRenderer([], _ => 20, showServiceBusCounts: true).CreateColumnLayout();

        // A six-digit queue depth (999999) must fit without truncation.
        Assert.True(WidthOf(layout, "Msgs") >= "999999".Length);
        Assert.True(WidthOf(layout, "DLQ") >= "999999".Length);
    }

    // The sort marker the header renderer (UiStyles.CreateHeaderText) appends to a sortable column:
    // "(n) ↓" for a single-digit column index. The column must be wide enough for header + marker or
    // the marker wraps onto a second terminal line.
    private const int SortMarkerWidth = 5;

    [Fact]
    public void FunctionApps_CountColumns_FitHeaderWithSortMarker()
    {
        var layout = new FunctionAppLayoutRenderer([], _ => 20, showServiceBusCounts: true).CreateColumnLayout();

        Assert.True(WidthOf(layout, "Msgs") >= "Msgs".Length + SortMarkerWidth);
        Assert.True(WidthOf(layout, "DLQ") >= "DLQ".Length + SortMarkerWidth);
    }

    [Fact]
    public void Functions_CountColumns_FitHeaderWithSortMarker()
    {
        var layout = new FunctionLayoutRenderer().CreateColumnLayout();

        Assert.True(WidthOf(layout, "Msgs") >= "Msgs".Length + SortMarkerWidth);
        Assert.True(WidthOf(layout, "DLQ") >= "DLQ".Length + SortMarkerWidth);
    }

    [Fact]
    public void Slots_StateColumn_FitsWidestStateLabel()
    {
        var layout = new FunctionAppSlotLayoutRenderer().CreateColumnLayout();
        var widest = StateLabels.Max(l => l.Length);

        Assert.True(WidthOf(layout, "State") >= widest);
    }

    [Fact]
    public void Functions_TriggerColumn_FitsWidestCommonTriggerName()
    {
        var layout = new FunctionLayoutRenderer().CreateColumnLayout();

        // Common Azure trigger binding types; "ServiceBusTrigger" (17) is the widest reported.
        string[] triggers =
        [
            "HttpTrigger", "TimerTrigger", "QueueTrigger", "BlobTrigger",
            "ServiceBusTrigger", "EventGridTrigger", "EventHubTrigger", "CosmosDBTrigger"
        ];
        var widest = triggers.Max(t => t.Length);

        Assert.True(WidthOf(layout, "Trigger") >= widest,
            $"Trigger column {WidthOf(layout, "Trigger")} < widest trigger {widest}");
    }

    [Fact]
    public void Functions_StateColumn_FitsEnabledDisabledLabels()
    {
        var layout = new FunctionLayoutRenderer().CreateColumnLayout();
        var widest = new[] { "Enabled", "Disabled" }.Max(l => l.Length);

        Assert.True(WidthOf(layout, "State") >= widest);
    }
}
