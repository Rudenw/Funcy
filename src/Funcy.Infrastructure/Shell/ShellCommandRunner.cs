namespace Funcy.Infrastructure.Shell;

using System.Diagnostics;

public static class ShellCommandRunner
{
    public static async Task<string> RunAsync(string command, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = GetShellExecutable(command),
            Arguments = GetShellArguments(command, arguments),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = psi;

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var errors = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command '{command} {arguments}' failed with exit code {process.ExitCode}:\n{errors}"
            );
        }

        return output.Trim();
    }

    private static string GetShellExecutable(string command)
    {
        return OperatingSystem.IsWindows() ? "cmd.exe" : command;
    }

    private static string GetShellArguments(string command, string arguments)
    {
        return OperatingSystem.IsWindows() ? $"/c {command} {arguments}" : arguments;
    }
}
