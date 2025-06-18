using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Mcp.CodeReview.Tools;

[McpServerToolType]
public static class CommandTools
{
    private static readonly string[] BlockedCommands = { 
        "rm", "del", "format", "mkfs", "dd", "shutdown", "reboot", "halt", 
        "sudo", "su", "chmod", "chown", "mount", "umount", "fdisk", "kill", "killall"
    };

    private static readonly Regex UnsafePatterns = new(
        @"(&&|\|\||;|`|\$\(|\$\{|>|>>|<|\||&|\n|\r)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    [McpServerTool, Description("Execute a shell command in the workspace")]
    public static async Task<object> RunCommand(
        [Description("The shell command to execute")] string command, 
        [Description("The working directory path")] string workingDirectory = "/data")
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(command))
            throw new InvalidOperationException("Command cannot be empty");

        if (command.Length > 1000)
            throw new InvalidOperationException("Command too long (max 1000 characters)");

        // Enhanced security checks
        if (BlockedCommands.Any(blocked => 
            Regex.IsMatch(command, $@"\b{Regex.Escape(blocked)}\b", RegexOptions.IgnoreCase)))
            throw new InvalidOperationException($"Command blocked for security: {command}");

        if (UnsafePatterns.IsMatch(command))
            throw new InvalidOperationException("Command contains unsafe characters or operators");

        // Path validation
        var fullWorkingDir = Path.GetFullPath(Path.Combine("/data", workingDirectory.TrimStart('/')));
        if (!fullWorkingDir.StartsWith("/data/") && fullWorkingDir != "/data")
            throw new InvalidOperationException("Working directory must be under /data");

        if (!Directory.Exists(fullWorkingDir))
            Directory.CreateDirectory(fullWorkingDir);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c",
                WorkingDirectory = fullWorkingDir,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) error.AppendLine(e.Data); };

        process.Start();
        
        // Write command to stdin instead of passing as argument
        await process.StandardInput.WriteLineAsync(command);
        process.StandardInput.Close();
        
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Add timeout to prevent hanging
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            throw new TimeoutException("Command execution timed out (5 minutes)");
        }

        return new
        {
            exitCode = process.ExitCode,
            stdout = output.ToString(),
            stderr = error.ToString(),
            command,
            workingDirectory = fullWorkingDir
        };
    }
}