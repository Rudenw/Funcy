using System.Collections.Concurrent;
using System.Diagnostics;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Settings;
using Funcy.Console.Ui;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly FuncySettings _settings;

    private CancellationTokenSource? _syncCts;
    private Task? _syncTask;

    private readonly ConcurrentDictionary<string, DateTime> _lastSubscriptionSyncUtc = new();

    public FunctionAppUpdateHandler(ILogger<FunctionAppUpdateHandler> logger,
        IAzureFunctionService functionService,
        FunctionStateCoordinator functionStateCoordinator,
        AnimationHandler animationHandler,
        IUiStatusState uiStatusState,
        FunctionStatusManager functionStatusManager,
        AppContext appContext,
        IOptions<FuncySettings> settings)
    {
        _logger = logger;
        _functionService = functionService;
        _functionStateCoordinator = functionStateCoordinator;
        _animationHandler = animationHandler;
        _uiStatusState = uiStatusState;
        _functionStatusManager = functionStatusManager;
        _appContext = appContext;
        _settings = settings.Value;

        _appContext.OnSubscriptionChange += OnSubscriptionChanged;
    }

    private async void OnSubscriptionChanged(SubscriptionDetails obj)
    {
        await CancelCurrentSyncAsync();

        await InitializeAsync();

        if (ShouldRefreshSubscription(obj.Id))
        {
            await SynchronizeFunctionAppDataAsync();
        }
    }

    private async Task CancelCurrentSyncAsync()
    {
        var oldCts = _syncCts;
        var oldTask = _syncTask;
        _syncCts = null;
        _syncTask = null;

        if (oldCts is not null)
        {
            try
            {
                await oldCts.CancelAsync();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed
            }
        }

        if (oldTask is not null)
        {
            try
            {
                await oldTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for old task during subscription change");
            }
        }

        oldCts?.Dispose();
    }

    private bool ShouldRefreshSubscription(string subscriptionId)
    {
        var intervalMinutes = _settings.SubscriptionRefreshIntervalMinutes;
        if (intervalMinutes == 0)
        {
            return true;
        }

        if (!_lastSubscriptionSyncUtc.TryGetValue(subscriptionId, out var lastSync))
        {
            return true;
        }

        return (DateTime.UtcNow - lastSync).TotalMinutes >= intervalMinutes;
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
        try
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            _animationHandler.AddAppDetails("TopPanel");
            _uiStatusState.BeginInventoryValidation();
            var functionAppDetailsToUpdate =
                _functionService.GetFunctionAppDetailsAsync(_appContext.CurrentSubscription.Id, token);

            await UpdateFunctionAppList(functionAppDetailsToUpdate, token);

            await _functionStateCoordinator.WaitForPendingUpdatesAsync();

            _uiStatusState.EndInventoryValidation();
            _animationHandler.RemoveAppDetails("TopPanel");

            _uiStatusState.BeginDetailsRefresh();
            await LoadAllDetailsInBackground(token);
            _uiStatusState.EndDetailsRefresh();

            _lastSubscriptionSyncUtc[_appContext.CurrentSubscription.Id] = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            // Expected when subscription changes
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout waiting for database lock - previous sync did not complete in time");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during synchronization");
            throw;
        }
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

    public async Task LoadAllDetailsAsync()
    {
        if (!CanRefreshAll()) return;

        _syncCts = new CancellationTokenSource();
        var token = _syncCts.Token;

        _syncTask = Task.Run(async () =>
        {
            try
            {
                _uiStatusState.BeginDetailsRefresh();
                await LoadAllDetailsInBackground(token);
                _uiStatusState.EndDetailsRefresh();
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during refresh all");
            }
        }, token);

        await _syncTask;
    }

    public bool CanRefreshAll()
    {
        var snapshot = _uiStatusState.GetSnapshot();
        return !snapshot.IsInventoryValidating && !snapshot.IsDetailsRefreshing;
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
                    await _functionStateCoordinator.PublishUpdateAsync(newApp.Details!, newApp.UpdateKind);
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
