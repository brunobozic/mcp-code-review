using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mcp.CodeReview.Tools;

[McpServerToolType]
public static class FileTools
{
    private const string AllowedRoot = "/data";
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private const int MaxDirectoryEntries = 1000;
    
    private static readonly Regex ValidPathPattern = new(@"^[a-zA-Z0-9._/\-\s]+$", RegexOptions.Compiled);
    private static readonly string[] DangerousExtensions = { ".sh", ".bat", ".cmd", ".exe", ".com", ".scr", ".pif" };

    private static string ValidateAndNormalizePath(string path, bool allowCreate = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty");

        if (path.Length > 260)
            throw new ArgumentException("Path too long (max 260 characters)");

        if (!ValidPathPattern.IsMatch(path))
            throw new ArgumentException("Path contains invalid characters");

        // Prevent path traversal attacks
        if (path.Contains("..") || path.Contains("~"))
            throw new ArgumentException("Path traversal not allowed");

        var normalizedPath = path.TrimStart('/').Replace('\\', '/');
        var fullPath = Path.GetFullPath(Path.Combine(AllowedRoot, normalizedPath));
        
        // Ensure path is within allowed root (handles Windows/Unix differences)
        var allowedRootFull = Path.GetFullPath(AllowedRoot);
        if (!fullPath.StartsWith(allowedRootFull + Path.DirectorySeparatorChar) && fullPath != allowedRootFull)
            throw new UnauthorizedAccessException("Access denied: path outside allowed root");

        return fullPath;
    }

    [McpServerTool, Description("List files in a directory")]
    public static async Task<object> ListFiles(
        [Description("The directory path to list files from")] string path = ".")
    {
        var fullPath = ValidateAndNormalizePath(path);

        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        try
        {
            var allEntries = Directory.GetFileSystemEntries(fullPath, "*", SearchOption.TopDirectoryOnly);
            
            if (allEntries.Length > MaxDirectoryEntries)
                throw new InvalidOperationException($"Too many entries in directory (max {MaxDirectoryEntries})");

            var files = Directory.GetFiles(fullPath, "*", SearchOption.TopDirectoryOnly)
                .Select(f => 
                {
                    var fileInfo = new FileInfo(f);
                    return new { 
                        name = fileInfo.Name, 
                        type = "file", 
                        size = fileInfo.Length,
                        lastModified = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    };
                });
            
            var directories = Directory.GetDirectories(fullPath, "*", SearchOption.TopDirectoryOnly)
                .Select(d => 
                {
                    var dirInfo = new DirectoryInfo(d);
                    return new { 
                        name = dirInfo.Name, 
                        type = "directory", 
                        size = 0L,
                        lastModified = dirInfo.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    };
                });

            return new { files = files.Concat(directories).OrderBy(x => x.name).ToArray() };
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException("Access denied to directory");
        }
        catch (IOException ex)
        {
            throw new IOException($"I/O error accessing directory: {ex.Message}");
        }
    }

    [McpServerTool, Description("Read contents of a file")]
    public static async Task<object> ReadFile(
        [Description("The file path to read")] string path)
    {
        var fullPath = ValidateAndNormalizePath(path);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {path}");

        try
        {
            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > MaxFileSize)
                throw new InvalidOperationException($"File too large (max {MaxFileSize / (1024 * 1024)}MB)");

            var content = await File.ReadAllTextAsync(fullPath);
            return new { 
                content, 
                path, 
                size = content.Length,
                lastModified = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException("Access denied to file");
        }
        catch (IOException ex)
        {
            throw new IOException($"I/O error reading file: {ex.Message}");
        }
    }

    [McpServerTool, Description("Write contents to a file")]
    public static async Task<object> WriteFile(
        [Description("The file path to write to")] string path, 
        [Description("The content to write to the file")] string content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content), "Content cannot be null");

        if (content.Length > MaxFileSize)
            throw new ArgumentException($"Content too large (max {MaxFileSize / (1024 * 1024)}MB)");

        var fullPath = ValidateAndNormalizePath(path, allowCreate: true);

        // Check for dangerous file extensions
        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        if (DangerousExtensions.Contains(extension))
            throw new InvalidOperationException($"Writing files with extension '{extension}' is not allowed");

        try
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(fullPath, content);
            
            var fileInfo = new FileInfo(fullPath);
            return new { 
                success = true, 
                path, 
                size = content.Length,
                lastModified = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException("Access denied to write file");
        }
        catch (IOException ex)
        {
            throw new IOException($"I/O error writing file: {ex.Message}");
        }
    }
}