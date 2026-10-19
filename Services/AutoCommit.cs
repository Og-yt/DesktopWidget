using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DesktopWidget.Services;

public static class GitCommitService
{
    public static async Task<string> CommitAndPushAsync(
        string targetPath,
        string commitMsg)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new ArgumentException(
                "Git リポジトリのフォルダーを指定してください。",
                nameof(targetPath));
        }

        var workingDirectory = Path.GetFullPath(targetPath.Trim());
        if (!Directory.Exists(workingDirectory))
        {
            throw new DirectoryNotFoundException(
                $"フォルダーが見つかりません: {workingDirectory}");
        }

        if (string.IsNullOrWhiteSpace(commitMsg))
        {
            throw new ArgumentException(
                "コミットメッセージを指定してください。",
                nameof(commitMsg));
        }

        var output = new StringBuilder();
        await RunGitAsync(workingDirectory, output, "status");
        await RunGitAsync(workingDirectory, output, "add", ".");
        await RunGitAsync(workingDirectory, output, "commit", "-m", commitMsg);
        await RunGitAsync(workingDirectory, output, "push");

        return output.ToString();
    }

    private static async Task RunGitAsync(
        string workingDirectory,
        StringBuilder output,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Git プロセスを起動できませんでした。");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        output.AppendLine($"> git {string.Join(" ", arguments)}");
        output.Append(standardOutput);
        output.Append(standardError);

        if (process.ExitCode != 0)
        {
            var details = string.IsNullOrWhiteSpace(standardError)
                ? standardOutput
                : standardError;
            throw new InvalidOperationException(
                $"git {string.Join(" ", arguments)} に失敗しました " +
                $"(終了コード: {process.ExitCode})。{Environment.NewLine}{details}");
        }
    }
}
