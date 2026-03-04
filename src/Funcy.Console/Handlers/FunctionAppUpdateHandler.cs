using System.Diagnostics;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Ui;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Microsoft.Extensions.Logging;

namespace Funcy.Console.Handlers;

public class FunctionAppUpdateHandler : IDetailsLoader
{
    private readonly ILogger<FunctionAppUpdateHandler> _logger;
    private readonly IAzureFunctionService _functionService;
    private readonly FunctionStateCoordinator _functionStateCoordinator;
    private readonly AnimationHandler _animationHandler;
    private readonly IUiStatusState _uiStatusState;
    private readonly FunctionStatusManager _functionStatusManager;
    private readonly AppContext _appContext;
    
    private CancellationTokenSource? _syncCts;
    private Task? _syncTask;

    public FunctionAppUpdateHandler(ILogger<FunctionAppUpdateHandler> logger,
        IAzureFunctionService functionService,
        FunctionStateCoordinator functionStateCoordinator,
        AnimationHandler animationHandler,
        IUiStatusState uiStatusState,
        FunctionStatusManager functionStatusManager,
        AppContext appContext)
    {
        _logger = logger;
        _functionService = functionService;
        _functionStateCoordinator = functionStateCoordinator;
        _animationHandler = animationHandler;
        _uiStatusState = uiStatusState;
        _functionStatusManager = functionStatusManager;
        _appContext = appContext;
        
        _appContext.OnSubscriptionChange += OnSubscriptionChanged;
    }

    private async void OnSubscriptionChanged(SubscriptionDetails obj)
    {
        if (_syncCts is not null)
        {
            await _syncCts.CancelAsync();
            _syncCts.Dispose();
        }
        
        if (_syncTask is not null)
        {
            try
            {
                await _syncTask;
            }
            catch (OperationCanceledException)
            {
                //Expected
            }
        }
        
        await InitializeAsync();
        await SynchronizeFunctionAppDataAsync();
    }
    
    public async Task InitializeAsync()
    {
        _functionStateCoordinator.SetSubscription(_appContext.CurrentSubscription.Id);
        var functionsFromDatabase =
            await _functionService.GetFunctionsFromDatabase(_appContext.CurrentSubscription.Id);
        _functionStateCoordinator.InitCache(functionsFromDatabase);
    }

    public async Task SynchronizeFunctionAppDataAsync()
    {
        _syncCts = new CancellationTokenSource();
        _syncTask = SynchronizeFunctionAppDataInternalAsync(_syncCts.Token);
        await _syncTask;
    }

    private async Task SynchronizeFunctionAppDataInternalAsync(CancellationToken token)
    {
        _animationHandler.AddAppDetails("TopPanel");
        _uiStatusState.BeginInventoryValidation();
        var functionAppDetailsToUpdate =
            _functionService.GetFunctionAppDetailsAsync(_appContext.CurrentSubscription.Id, token);
        await UpdateFunctionAppList(functionAppDetailsToUpdate, token);
        _uiStatusState.EndInventoryValidation();
        _animationHandler.RemoveAppDetails("TopPanel");

        _uiStatusState.BeginDetailsRefresh();
        await LoadAllDetailsInBackground(token);
        _uiStatusState.EndDetailsRefresh();
    }
    
    private async Task LoadAllDetailsInBackground(CancellationToken token)
    {
        var allApps = _functionStateCoordinator.GetCachedFunctionAppDetails();
        _uiStatusState.SetTotalDetails(allApps.Count);
        var updatedFunctionApps = _functionService.GetFunctionAppFunctionsAndSlotsAsync(allApps, token);
        await UpdateFunctionAppList(updatedFunctionApps, token);
    }
    
    public void LoadDetails(string currentKey)
    {
        var functionAppDetails = _functionStateCoordinator.TryGet(currentKey);
        if (functionAppDetails is null) return;
        
        _ = Task.Run(async () =>
        {
            await _functionStatusManager.BeginOperation(functionAppDetails, FunctionAction.Refresh);
            var updatedDetails = await _functionService.GetFunctionAppDetails(functionAppDetails);
            await _functionStatusManager.CompleteOperation(updatedDetails, FunctionAction.Refresh, true);
        });
    }

    private async Task UpdateFunctionAppList(IAsyncEnumerable<FunctionAppFetchResult> functionAppDetailsToUpdate,
        CancellationToken syncCtsToken)
    {
        List<string> existingFunctionAppNames = [];
        var sw = Stopwatch.StartNew();
        try
        {
            await foreach (var newApp in functionAppDetailsToUpdate.WithCancellation(syncCtsToken))
            {
                existingFunctionAppNames.Add(newApp.Name);
                if (newApp.IsSuccess)
                {
                    await _functionStateCoordinator.PublishUpdateAsync(newApp.Details!);
                    _uiStatusState.IncrementDetailsInFlight();
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UpdateFunctionAppList was cancelled");
        }
        finally
        {
            _uiStatusState.ResetDetailsInFlight();
            sw.Stop();
            _logger.LogInformation("Updated Function App List in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

            if (existingFunctionAppNames.Count > 0)
            {
                await _functionStateCoordinator.RemoveFunctions(existingFunctionAppNames);
            }
        }
    }
}