namespace Funcy.Core.Interfaces;

// Copies text to / reads text from the operating system clipboard. Never throws: a missing
// clipboard tool yields false (copy) or null (read) so callers can degrade gracefully.
public interface IClipboardService
{
    Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default);

    // The current clipboard text, or null when no clipboard tool is available or it is empty.
    Task<string?> TryReadAsync(CancellationToken cancellationToken = default);
}
