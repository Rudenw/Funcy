using Funcy.Console.Data;
using Funcy.Console.Ui.Factories;
using Funcy.Console.Ui.Factories.Models;
using Funcy.Console.Ui.Pagination;
using Funcy.Console.Ui.Renderers;
using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.Panels;

public class SlotPanel : IBodyPanelController
{
    private readonly Dictionary<string, TableRowMarkup> _markupCache = [];
    private List<TableRowMarkup> _visibleRows = [];
    
    public Panel Panel { get; private set; }
    
    private readonly FunctionAppPaginator _paginator;
    private readonly FunctionAppTableRenderer _renderer;
    
    public event Action? OnSwap;

    public SlotPanel(List<FunctionAppSlotDetails> slotDetails)
    {
        _paginator = new FunctionAppPaginator();
        _renderer = new FunctionAppTableRenderer(CreateTableColumns());
        Panel = new Panel(_renderer.Table)
            .Header("Deployment Slots", Justify.Center)
            .BorderColor(Color.Orange1);
        UpdateData(slotDetails);
    }

    private List<(Func<TableRowMarkup, bool, Markup> selector, string columnName)> CreateTableColumns()
    {
        return
        [
            ((t, isSelected) => t.GetName(isSelected), "Name"),
            ((t, isSelected) => t.GetState(isSelected), "State")
        ];
    }
    
    public void UpdateData(List<FunctionAppSlotDetails> slotDetails)
    {
        _paginator.UpdateTotalRows(slotDetails.Count);
        BuildCache(slotDetails);
        RefreshView();
    }

    public void HandleInput(ConsoleKeyInfo keyInfo)
    {
        var scrolled = keyInfo.Key switch
        {
            ConsoleKey.UpArrow   => _paginator.MoveUp(),
            ConsoleKey.DownArrow => _paginator.MoveDown(),
            _                     => false
        };

        _renderer.Render(_visibleRows, _paginator.SelectedIndex);
    }
    
    public FunctionAppSlotDetails GetSelectedSlot()
    {
        var selectedSlot = _visibleRows[_paginator.SelectedIndex].SlotDetails;
        return selectedSlot;
    }

    public void SwapFunction()
    {
        OnSwap?.Invoke();
    }

    private void RefreshView()
    {
        _visibleRows = _markupCache.Values.ToList();
        _renderer.Render(_visibleRows, _paginator.SelectedIndex);
    }
    
    private void BuildCache(IEnumerable<FunctionAppSlotDetails> slotDetails)
    {
        foreach (var app in slotDetails)
        {
            _markupCache[app.FullName] = TableRowMarkupFactory.Create(app);
        }
    }
}