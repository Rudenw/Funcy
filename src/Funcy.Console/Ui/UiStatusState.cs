namespace Funcy.Console.Ui;

public interface IUiStatusState
{
    event Action? Changed;

    UiStatusSnapshot GetSnapshot();

    void BeginInventoryRefresh();
    void EndInventoryRefresh();

    void IncrementDetailsInFlight();
    void DecrementDetailsInFlight();

    void SetLastError(string? message);
}

public readonly struct UiStatusSnapshot
{
    public bool IsInventoryRefreshing { get; init; }
    public int DetailsInFlight { get; init; }
    public long LastInventoryRefreshUtcTicks { get; init; }
    public long LastErrorUtcTicks { get; init; }
    public string? LastError { get; init; }
}

public sealed class UiStatusState : IUiStatusState
{
    public event Action? Changed;

    private int _isInventoryRefreshing;
    private int _detailsInFlight;

    private long _lastInventoryRefreshUtcTicks;
    private long _lastErrorUtcTicks;
    private string? _lastError;

    private int _changeQueued;

    public UiStatusSnapshot GetSnapshot()
    {
        return new UiStatusSnapshot
        {
            IsInventoryRefreshing = Volatile.Read(ref _isInventoryRefreshing) == 1,
            DetailsInFlight = Volatile.Read(ref _detailsInFlight),
            LastInventoryRefreshUtcTicks = Volatile.Read(ref _lastInventoryRefreshUtcTicks),
            LastErrorUtcTicks = Volatile.Read(ref _lastErrorUtcTicks),
            LastError = Volatile.Read(ref _lastError)
        };
    }

    public void BeginInventoryRefresh()
    {
        if (Interlocked.Exchange(ref _isInventoryRefreshing, 1) == 1)
            return;

        QueueChanged();
    }

    public void EndInventoryRefresh()
    {
        if (Interlocked.Exchange(ref _isInventoryRefreshing, 0) == 0)
            return;

        Volatile.Write(ref _lastInventoryRefreshUtcTicks, DateTime.UtcNow.Ticks);
        QueueChanged();
    }

    public void IncrementDetailsInFlight()
    {
        Interlocked.Increment(ref _detailsInFlight);
        QueueChanged();
    }

    public void DecrementDetailsInFlight()
    {
        var after = Interlocked.Decrement(ref _detailsInFlight);
        if (after < 0)
            Interlocked.Exchange(ref _detailsInFlight, 0);

        QueueChanged();
    }

    public void SetLastError(string? message)
    {
        Volatile.Write(ref _lastError, message);
        Volatile.Write(ref _lastErrorUtcTicks, message is null ? 0 : DateTime.UtcNow.Ticks);
        QueueChanged();
    }

    private void QueueChanged()
    {
        if (Interlocked.Exchange(ref _changeQueued, 1) == 1)
            return;

        ThreadPool.QueueUserWorkItem(static state =>
        {
            var self = (UiStatusState)state!;
            Interlocked.Exchange(ref self._changeQueued, 0);
            self.Changed?.Invoke();
        }, this);
    }
}