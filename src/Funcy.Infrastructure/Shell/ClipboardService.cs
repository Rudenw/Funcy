using System.Diagnostics;
using Funcy.Core.Interfaces;

namespace Funcy.Infrastructure.Shell;

// Copies text to the OS clipboard by piping it to the platform clipboard tool's stdin:
// Windows -> clip, macOS -> pbcopy, Linux -> wl-copy (Wayland) then xclip (X11) as a fallback.
// A missing tool or a failed pipe returns false rather than throwing, so the UI stays alive on
// machines with no clipboard integration (e.g. a bare SSH session).
public sealed class ClipboardService : IClipboardService
{
    public async Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default)
    {
        foreach (var (file, arguments) in Candidates())
        {
            if (await TryCopyWithAsync(file, arguments, text, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<string?> TryReadAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (file, arguments) in ReadCandidates())
        {
            var text = await TryReadWithAsync(file, arguments, cancellationToken);
            if (text is not null)
            {
                return text;
            }
        }

        return null;
    }

    private static IEnumerable<(string File, string Arguments)> Candidates()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return ("clip", string.Empty);
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return ("pbcopy", string.Empty);
        }
        else
        {
            yield return ("wl-copy", string.Empty);
            yield return ("xclip", "-selection clipboard");
        }
    }

    private static IEnumerable<(string File, string Arguments)> ReadCandidates()
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows has no read-side 'clip'; PowerShell's Get-Clipboard is the standard fallback.
            yield return ("powershell", "-NoProfile -Command Get-Clipboard");
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return ("pbpaste", string.Empty);
        }
        else
        {
            yield return ("wl-paste", "--no-newline");
            yield return ("xclip", "-selection clipboard -o");
        }
    }

    private static async Task<string?> TryReadWithAsync(string file, string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            if (!process.Start())
            {
                return null;
            }

            var readTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = await readTask;

            if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
            {
                return null;
            }

            // Get-Clipboard appends a trailing newline; drop only trailing line breaks.
            return output.TrimEnd('\r', '\n');
        }
        catch (Exception)
        {
            // Tool not on PATH or the read failed — let the caller try the next one.
            return null;
        }
    }

    private static async Task<bool> TryCopyWithAsync(string file, string arguments, string text, CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            if (!process.Start())
            {
                return false;
            }

            await process.StandardInput.WriteAsync(text.AsMemory(), cancellationToken);
            process.StandardInput.Close();
            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            // Tool not on PATH (Win32Exception) or the pipe broke — let the caller try the next one.
            return false;
        }
    }
}
