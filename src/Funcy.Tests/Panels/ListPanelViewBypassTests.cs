using Funcy.Console.Handlers;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui.PanelLayout;
using Funcy.Console.Ui.PanelLayout.Renderers;
using Funcy.Console.Ui.Pagination.Matchers;
using Funcy.Console.Ui.Panels;
using Funcy.Console.Ui.Shortcuts;
using Funcy.Core.Model;
using Spectre.Console;
using Xunit;

namespace Funcy.Tests.Panels;

public class ListPanelViewBypassTests
{
    [Fact]
    public void RebuildVisibleRows_NonMatchingRowWithActiveOperation_StaysVisibleUnderFilter()
    {
        var view = MakeView(new TrackingLayoutRenderer());
        view.SetAll([new OpItem("opti-app", active: false), new OpItem("extenda-app", active: true)]);

        view.SetSearchText("opti");

        Assert.Contains("extenda-app", view.GetVisibleKeys());
        Assert.Contains("opti-app", view.GetVisibleKeys());
    }

    [Fact]
    public void RebuildVisibleRows_BypassedRow_DisappearsWhenStatusReturnsToIdleViaUpsert()
    {
        var view = MakeView(new TrackingLayoutRenderer());
        view.SetAll([new OpItem("opti-app", active: false), new OpItem("extenda-app", active: true)]);
        view.SetSearchText("opti");
        Assert.Contains("extenda-app", view.GetVisibleKeys());

        // TTL reset republishes the app as Idle through Upsert; the bypass must self-clear.
        view.Upsert(new OpItem("extenda-app", active: false));
        view.RenderIfNeeded();

        Assert.DoesNotContain("extenda-app", view.GetVisibleKeys());
        Assert.Contains("opti-app", view.GetVisibleKeys());
    }

    [Fact]
    public void RebuildVisibleRows_MatchingRows_AreNotTreatedAsBypassed()
    {
        var layout = new TrackingLayoutRenderer();
        var view = MakeView(layout);
        view.SetAll([new OpItem("opti-app", active: true), new OpItem("extenda-app", active: true)]);

        view.SetSearchText("opti");

        // opti-app matches the filter, so it uses the cached markup, never the bypass path.
        Assert.DoesNotContain("opti-app", layout.BypassKeys);
        Assert.Contains("opti-app", view.GetVisibleKeys());
    }

    [Fact]
    public void RebuildVisibleRows_BypassedRow_UsesBypassMarkupPath()
    {
        var layout = new TrackingLayoutRenderer();
        var view = MakeView(layout);
        view.SetAll([new OpItem("opti-app", active: false), new OpItem("extenda-app", active: true)]);

        view.SetSearchText("opti");

        // The bypassed row is rendered through the dedicated (never-highlighted) bypass path.
        Assert.Contains("extenda-app", layout.BypassKeys);
    }

    [Fact]
    public void RebuildVisibleRows_BypassedRows_FloatToTopAboveMatches()
    {
        var view = MakeView(new TrackingLayoutRenderer());
        view.SetAll([
            new OpItem("aaa-match", active: false),
            new OpItem("bbb-match", active: false),
            new OpItem("zzz-active", active: true), // sorts last, does not match "match"
        ]);

        view.SetSearchText("match");

        Assert.Equal(["zzz-active", "aaa-match", "bbb-match"], view.GetVisibleKeys());
    }

    [Fact]
    public void RebuildVisibleRows_EmptyFilter_ShowsAllWithoutBypassMarkup()
    {
        var layout = new TrackingLayoutRenderer();
        var view = MakeView(layout);
        view.SetAll([new OpItem("opti-app", active: false), new OpItem("extenda-app", active: true)]);

        view.RenderIfNeeded();

        // Everything fits on screen, so nothing floats — plain sorted order, no dim bypass markup.
        Assert.Equal(["extenda-app", "opti-app"], view.GetVisibleKeys());
        Assert.Empty(layout.BypassKeys);
    }

    [Fact]
    public void RebuildVisibleRows_NoFilter_OnScreenActiveRow_DoesNotFloat()
    {
        var layout = new TrackingLayoutRenderer();
        var view = MakeView(layout);
        view.SetAll([
            new OpItem("aaa-idle", active: false),
            new OpItem("mmm-idle", active: false),
            new OpItem("zzz-active", active: true), // sorts last, has an active operation, but is visible
        ]);

        view.RenderIfNeeded();

        // The active row is already on screen, so acting on it must not make it jump to the top.
        Assert.Equal(["aaa-idle", "mmm-idle", "zzz-active"], view.GetVisibleKeys());
        Assert.Empty(layout.BypassKeys);
    }

    [Fact]
    public void RebuildVisibleRows_NoFilter_OffScreenActiveRow_FloatsToTop()
    {
        var layout = new TrackingLayoutRenderer();
        var view = MakeView(layout);

        // A list larger than the window (MaxVisibleRows = windowHeight 30 - 8 = 22) with the only
        // active row sorted last, so it starts off-screen and must float up to be watchable.
        var items = new List<OpItem>();
        for (var i = 0; i < 25; i++)
        {
            items.Add(new OpItem($"app-{i:00}", active: false));
        }
        items.Add(new OpItem("zzz-active", active: true));
        view.SetAll(items);

        view.RenderIfNeeded();

        Assert.Equal("zzz-active", view.GetVisibleKeys()[0]);
        // Floated (unfiltered) rows are not "bypassed", so no dim bypass markup is produced.
        Assert.Empty(layout.BypassKeys);
    }

    private static ListPanelView<OpItem> MakeView(TrackingLayoutRenderer layout)
    {
        return new ListPanelView<OpItem>(
            new KeyMatcher(),
            layout,
            new NoShortcuts(),
            new NoAnimations(),
            onEnterNavigation: null,
            header: "Test",
            onAction: null,
            onActionNavigation: null,
            emptyStateMessage: null,
            windowHeight: () => 30);
    }

    private sealed record OpItem(string Key, bool active) : IHasKey, IComparable<OpItem>, IOperationVisibility
    {
        public bool HasActiveOperation => active;
        public int CompareTo(OpItem? other) => string.CompareOrdinal(Key, other?.Key);
    }

    private sealed class TrackingLayoutRenderer : ILayoutRenderer<OpItem>
    {
        public readonly List<string> BypassKeys = [];

        public RowMarkup CreateRowMarkup(OpItem item) => Row(item.Key);

        public RowMarkup CreateBypassRowMarkup(OpItem item)
        {
            BypassKeys.Add(item.Key);
            return Row(item.Key);
        }

        public ColumnLayout<OpItem> CreateColumnLayout() => new(new Column<OpItem>("Name", i => i.Key));

        private static RowMarkup Row(string key)
            => new RowMarkup { Key = key }.Add("Name", new RowCell(new Markup(key), new Markup(key)));
    }

    private sealed class KeyMatcher : ISearchMatcher<OpItem>
    {
        public bool TryMatch(OpItem app, string input)
            => app.Key.Contains(input, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class NoAnimations : IAnimationProvider
    {
        public List<AnimationContext> GetAnimations() => [];
        public AnimationContext? GetAnimation(string key) => null;
    }

    private sealed class NoShortcuts : IShortcutProvider<OpItem>
    {
        public Dictionary<TableIndex, ShortcutMap> Describe(OpItem? item) => new();
        public bool IsActionValid(OpItem? item, FunctionAction action) => false;
    }
}
