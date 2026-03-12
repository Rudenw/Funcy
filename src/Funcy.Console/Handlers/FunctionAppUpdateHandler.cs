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
    private readonly TimeSpan _subscriptionRefreshInterval;
    private readonly ConcurrentDictionary<string, DateTime> _lastSubscriptionSyncUtc = [];

    private CancellationTokenSource? _syncCts;
    private Task? _syncTask;

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
        _subscriptionRefreshInterval = TimeSpan.FromMinutes(Math.Max(0, settings.Value.SubscriptionRefreshIntervalMinutes));

        _appContext.OnSubscriptionChange += OnSubscriptionChanged;
    }

    private async void OnSubscriptionChanged(SubscriptionDetails obj)
    {
        await CancelCurrentSyncAsync();
        await InitializeAsync();

        if (ShouldRefreshSubscription(_appContext.CurrentSubscription.Id))
        {
            await SynchronizeFunctionAppDataAsync();
        }
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
        if (!CanRefreshAll())
        {
            return;
        }

        var subscriptionId = _appContext.CurrentSubscription.Id;
        var cts = new CancellationTokenSource();
        _syncCts = cts;
        _syncTask = SynchronizeFunctionAppDataInternalAsync(cts.Token);

        try
        {
            await _syncTask;

            if (!cts.IsCancellationRequested)
            {
                _lastSubscriptionSyncUtc[subscriptionId] = DateTime.UtcNow;
            }
        }
        finally
        {
            if (ReferenceEquals(_syncCts, cts))
            {
                _syncCts = null;
                _syncTask = null;
            }

            cts.Dispose();
        }
    }

    public Task LoadAllDetailsAsync()
    {
        return SynchronizeFunctionAppDataAsync();
    }

    public bool CanRefreshAll()
    {
        var status = _uiStatusState.GetSnapshot();
        return !status.IsInventoryValidating && !status.IsDetailsRefreshing;
    }

    private bool ShouldRefreshSubscription(string subscriptionId)
    {
        if (!_lastSubscriptionSyncUtc.TryGetValue(subscriptionId, out var lastRefreshUtc))
        {
            return true;
        }

        if (_subscriptionRefreshInterval == TimeSpan.Zero)
        {
            return true;
        }

        return DateTime.UtcNow - lastRefreshUtc >= _subscriptionRefreshInterval;
    }

    private async Task CancelCurrentSyncAsync()
    {
        if (_syncCts is not null)
        {
            await _syncCts.CancelAsync();
        }

        var oldTask = _syncTask;
        var oldCts = _syncCts;
        _syncTask = null;
        _syncCts = null;

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
