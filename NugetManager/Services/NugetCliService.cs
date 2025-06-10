using System.Diagnostics;

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
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
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

                    foreach (var line in output.Split('\n'))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || !trimmed.StartsWith(packageName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            var version = parts[1];
                            result.Add((version, true)); // CLI只显示Listed版本
                        }
                    }
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

    /// <summary>
    /// 使用NuGet CLI校准版本状态
    /// </summary>
    public async Task CalibrateVersionStatusWithNuGetCli(string packageName, List<(string Version, bool Listed)> result)
    {
        if (result.Count == 0) return;

        try
        {
            logAction?.Invoke("🔧 Calibrating version status using NuGet CLI...");
            logAction?.Invoke($"  Original versions found: {result.Count}");

            // 获取 NuGet CLI 的 Listed 版本列表
            var cliListedVersions = new List<(string Version, bool Listed)>();
            await TryNugetExeListCommand(packageName, cliListedVersions);

            logAction?.Invoke($"  NuGet CLI found: {cliListedVersions.Count} Listed versions");

            if (cliListedVersions.Count == 0)
            {
                logAction?.Invoke("⚠️  NuGet CLI found no versions, skipping calibration");
                return;
            }

            // 创建 Listed 版本的查找集合
            var listedVersionSet = new HashSet<string>(
                cliListedVersions.Select(v => v.Version),
                StringComparer.OrdinalIgnoreCase
            );

            logAction?.Invoke($"  Starting calibration against {listedVersionSet.Count} CLI Listed versions...");

            // 校准所有版本的状态
            var correctedCount = 0;
            for (var i = 0; i < result.Count; i++)
            {
                var (version, originalListed) = result[i];
                var actualListed = listedVersionSet.Contains(version);

                // 如果状态有变化，更新并记录
                if (originalListed == actualListed) continue;
                result[i] = (version, actualListed);
                correctedCount++;
                Debug.WriteLine($"Status corrected: {version} - Was: {originalListed}, Now: {actualListed}");
            }

            var listedCount = result.Count(r => r.Listed);
            var unlistedCount = result.Count - listedCount;

            logAction?.Invoke("✓ Status calibration completed");
            logAction?.Invoke($"  Corrections made: {correctedCount}");
            logAction?.Invoke($"  Final Listed: {listedCount}, Unlisted: {unlistedCount}");
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"⚠️  Status calibration failed: {ex.Message}");
            Debug.WriteLine($"CalibrateVersionStatusWithNuGetCli failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取包的版本列表
    /// </summary>
    public async Task<List<string>> ListPackageVersionsAsync(string packageName, bool includePrerelease = false)
    {
        var result = new List<string>();

        try
        {
            var nugetExe = FindNugetExe();
            if (string.IsNullOrEmpty(nugetExe))
            {
                logAction?.Invoke("× NuGet CLI not found");
                return result;
            }

            await Task.Run(() =>
            {
                try
                {
                    var args = $"list {packageName} -AllVersions";
                    if (includePrerelease)
                        args += " -PreRelease";

                    var psi = new ProcessStartInfo(nugetExe, args)
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process == null) return;

                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0) return;

                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && parts[0].Equals(packageName, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(parts[1]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logAction?.Invoke($"× NuGet list command failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"× ListPackageVersionsAsync failed: {ex.Message}");
        }

        return result;
    }
}
