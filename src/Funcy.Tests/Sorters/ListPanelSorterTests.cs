using Funcy.Console.Ui.PanelLayout;
using Funcy.Console.Ui.Pagination.Sorters;

namespace Funcy.Tests.Sorters;

public class ListPanelSorterTests
{
    private static ListPanelSorter<string> MakeSorter(int columnCount = 3)
    {
        var columns = Enumerable.Range(1, columnCount)
            .Select(i => new Column<string>($"Col{i}", s => s))
            .ToArray();
        return new ListPanelSorter<string>(new ColumnLayout<string>(columns));
    }

    [Fact]
    public void InitialState_HasNoColumnSelected()
    {
        var sorter = MakeSorter();
        Assert.Null(sorter.CurrentColumn);
        Assert.False(sorter.Desc);
    }

    [Fact]
    public void Toggle_NewColumn_SetsColumnAscending()
    {
        var sorter = MakeSorter();
        sorter.Toggle(1);
        Assert.Equal(1, sorter.CurrentColumn);
        Assert.False(sorter.Desc);
    }

    [Fact]
    public void Toggle_SameColumn_FlipsToDescending()
    {
        var sorter = MakeSorter();
        sorter.Toggle(1);
        sorter.Toggle(1);
        Assert.Equal(1, sorter.CurrentColumn);
        Assert.True(sorter.Desc);
    }

    [Fact]
    public void Toggle_SameColumn_ThirdTime_ClearsSort()
    {
        var sorter = MakeSorter();
        sorter.Toggle(1);
        sorter.Toggle(1);
        sorter.Toggle(1);
        Assert.Null(sorter.CurrentColumn);
        Assert.False(sorter.Desc);
    }

    [Fact]
    public void Toggle_DifferentColumn_ResetsToPreviousColumn()
    {
        var sorter = MakeSorter();
        sorter.Toggle(1);
        sorter.Toggle(1); // now Desc = true
        sorter.Toggle(2);
        Assert.Equal(2, sorter.CurrentColumn);
        Assert.False(sorter.Desc);
    }

    [Fact]
    public void Toggle_InvalidIndex_IsNoOp()
    {
        var sorter = MakeSorter();
        sorter.Toggle(99);
        Assert.Null(sorter.CurrentColumn);
        Assert.False(sorter.Desc);
    }

    [Fact]
    public void Sort_WithNoColumn_ReturnsSourceInOriginalOrder()
    {
        var sorter = MakeSorter();
        var source = new List<string> { "b", "a", "c" };
        var result = sorter.Sort(source);
        Assert.Equal(source, result);
    }

    [Fact]
    public void Sort_Ascending_OrdersBySelector()
    {
        var sorter = MakeSorter();
        sorter.Toggle(1);
        var result = sorter.Sort(["c", "a", "b"]);
        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void Sort_Descending_OrdersByDescSelector()
    {
        var sorter = MakeSorter();
        sorter.Toggle(1);
        sorter.Toggle(1); // Desc = true
        var result = sorter.Sort(["c", "a", "b"]);
        Assert.Equal(["c", "b", "a"], result);
    }
}
