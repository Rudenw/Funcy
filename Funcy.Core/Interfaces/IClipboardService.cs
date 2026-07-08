namespace Funcy.Core.Interfaces;

// Copies text to the operating system clipboard. Returns false (never throws) when no clipboard
// tool is available so callers can degrade gracefully.
public interface IClipboardService
{
    Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default);
}
