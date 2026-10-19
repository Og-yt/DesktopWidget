using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DesktopWidget.Services;


public sealed class CommandService
{
    public async Task<string> ExecuteAsync(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return "コマンドを入力してください。";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            Arguments = "/d /s /c \"" + command + "\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile)
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var output = await stdout;
        var error = await stderr;

        return
            $"Exit code: {process.ExitCode}{Environment.NewLine}" +
            output +
            (string.IsNullOrWhiteSpace(error)
                ? string.Empty
                : $"{Environment.NewLine}[stderr]{Environment.NewLine}{error}");
    }
}
