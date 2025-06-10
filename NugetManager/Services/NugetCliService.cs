using System.Diagnostics;
using System.Reflection;

namespace NugetManager.Services;

/// <summary>
/// 处理NuGet CLI相关的功能
/// </summary>
public class NugetCliService(Action<string>? logAction = null)
{
    // Static field to cache the nuget.exe path for reuse
    private static string? _cachedNugetExePath;
    private static readonly Lock _nugetPathLock = new();

    /// <summary>
    /// 查找nuget.exe的路径
    /// </summary>
    public static string? FindNugetExe()
    {
        lock (_nugetPathLock)
        {
            if (!string.IsNullOrEmpty(_cachedNugetExePath) && File.Exists(_cachedNugetExePath))
            {
                return _cachedNugetExePath;
            }

            // Try to extract from embedded resource first
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "NugetManager", "nuget.exe");
                var tempDir = Path.GetDirectoryName(tempPath);
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir!);
                }
                if (!File.Exists(tempPath))
                {
                    // Extract nuget.exe from embedded resource
                    var assembly = Assembly.GetExecutingAssembly();
                    const string resourceName = "NugetManager.Resources.nuget.exe";
                    using var resourceStream = assembly.GetManifestResourceStream(resourceName);
                    if (resourceStream != null)
                    {
                        using var fileStream = File.Create(tempPath);
                        resourceStream.CopyTo(fileStream);
                        _cachedNugetExePath = tempPath;
                        return _cachedNugetExePath;
                    }
                }
                else
                {
                    _cachedNugetExePath = tempPath;
                    return _cachedNugetExePath;
                }
            }
            catch
            {
                // Fallback to searching in standard locations
            }

            // Search in common locations
            var possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "nuget.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NuGet", "nuget.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NuGet", "nuget.exe"),
                "nuget.exe" // Check if it's in PATH
            };
            foreach (var path in possiblePaths)
            {
                if (!File.Exists(path)) continue;
                _cachedNugetExePath = path;
                return _cachedNugetExePath;
            }

            // Try to find via PATH
            try
            {
                var psi = new ProcessStartInfo("where", "nuget.exe")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        var path = output.Trim().Split('\n')[0].Trim();
                        if (File.Exists(path))
                        {
                            _cachedNugetExePath = path;
                            return _cachedNugetExePath;
                        }
                    }
                }
            }
            catch
            {
                // Ignore
            }
            return null;
        }
    }

    /// <summary>
    /// 使用NuGet CLI策略查询版本
    /// </summary>
    public async Task UseNuGetCliStrategy(string packageName, List<(string Version, bool Listed)> result)
    {
        logAction?.Invoke("🛠️ Trying NuGet CLI Tool...");
        await TryNugetExeListCommand(packageName, result);
        logAction?.Invoke($"   ✓ Found {result.Count} versions via NuGet CLI");
    }

    /// <summary>
    /// 使用nuget.exe list命令查询版本
    /// </summary>
    private async Task TryNugetExeListCommand(string packageName, List<(string Version, bool Listed)> result)
    {
        try
        {
            var nugetExe = FindNugetExe();
            if (string.IsNullOrEmpty(nugetExe))
            {
                logAction?.Invoke("   × NuGet CLI not found");
                return;
            }
            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo(nugetExe, $"list {packageName} -AllVersions -PreRelease")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var process = Process.Start(psi);
                    if (process == null) return;
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        logAction?.Invoke($"   × NuGet CLI failed: {error}");
                        return;
                    }
                    result.AddRange(from line in output.Split('\n')
                                    select line.Trim() into trimmed
                                    where !string.IsNullOrEmpty(trimmed) && trimmed.StartsWith(packageName, StringComparison.OrdinalIgnoreCase)
                                    select trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries) into parts
                                    where parts.Length >= 2
                                    select parts[1] into version
                                    select (version, true));
                }
                catch (Exception ex)
                {
                    logAction?.Invoke($"   × NuGet CLI error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × NuGet CLI failed: {ex.Message}");
        }
    }
}