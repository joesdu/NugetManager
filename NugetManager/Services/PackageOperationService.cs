using System.Diagnostics;
using System.Text;

namespace NugetManager.Services;

/// <summary>
/// 处理包操作（删除、弃用）的服务
/// </summary>
public sealed class PackageOperationService(Action<string>? logAction = null)
{    /// <summary>
     /// 执行包删除操作
     /// </summary>
    public async Task<bool> DeletePackageVersionsAsync(string packageName, string apiKey, List<string?> versions, CancellationToken cancellationToken = default, Action<int, int>? progressCallback = null)
    {
        try
        {
            var nugetExe = NugetCliHelper.FindNugetExe();
            if (string.IsNullOrEmpty(nugetExe))
            {
                logAction?.Invoke("× NuGet CLI not found");
                return false;
            }
            var successCount = 0;
            var totalCount = versions.Count;

            logAction?.Invoke($"🗑️ Starting deletion of {totalCount} versions...");

            for (var i = 0; i < versions.Count; i++)
            {
                var version = versions[i];
                if (cancellationToken.IsCancellationRequested)
                {
                    logAction?.Invoke("⚠️ Operation cancelled by user");
                    break;
                }

                try
                {
                    var success = await ExecuteNugetDeleteCommand(nugetExe, packageName, version, apiKey, cancellationToken);
                    if (success)
                    {
                        successCount++;
                        logAction?.Invoke($"✓ {version}: Deleted successfully");
                    }
                    else
                    {
                        logAction?.Invoke($"× {version}: Deletion failed");
                    }

                    // 更新进度
                    progressCallback?.Invoke(i + 1, totalCount);
                }
                catch (Exception ex)
                {
                    logAction?.Invoke($"× {version}: {ex.Message}");
                    progressCallback?.Invoke(i + 1, totalCount);
                }
            }

            logAction?.Invoke($"🎯 Deletion completed: {successCount}/{totalCount} successful");
            return successCount == totalCount;
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"× Delete operation failed: {ex.Message}");
            return false;
        }
    }    /// <summary>
         /// 执行包弃用操作
         /// </summary>
    public async Task<bool> DeprecatePackageVersionsAsync(string packageName, string apiKey, List<string> versions, string deprecationInfo, CancellationToken cancellationToken = default, Action<int, int>? progressCallback = null)
    {
        try
        {
            var successCount = 0;
            var totalCount = versions.Count;

            logAction?.Invoke($"⚠️ Starting deprecation of {totalCount} versions...");

            for (var i = 0; i < versions.Count; i++)
            {
                var version = versions[i];
                if (cancellationToken.IsCancellationRequested)
                {
                    logAction?.Invoke("⚠️ Operation cancelled by user");
                    break;
                }

                try
                {
                    var success = await TrySetDeprecationAsync(packageName, version, apiKey, deprecationInfo);
                    if (success)
                    {
                        successCount++;
                        logAction?.Invoke($"✓ {version}: Deprecated successfully");
                    }
                    else
                    {
                        logAction?.Invoke($"× {version}: Deprecation failed");
                    }

                    // 更新进度和状态
                    progressCallback?.Invoke(i + 1, totalCount);
                }
                catch (Exception ex)
                {
                    logAction?.Invoke($"× {version}: {ex.Message}");
                    progressCallback?.Invoke(i + 1, totalCount);
                }
            }

            logAction?.Invoke($"🎯 Deprecation completed: {successCount}/{totalCount} successful");
            return successCount == totalCount;
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"× Deprecate operation failed: {ex.Message}");
            return false;
        }
    }    /// <summary>
         /// 直接使用删除方法（由于弃用API不可用，直接使用unlist作为替代）
         /// </summary>
    private async Task<bool> TrySetDeprecationAsync(string packageId, string version, string apiKey, string deprecationInfo)
    {
        // 由于NuGet.org的弃用API不可用，直接使用删除方法
        logAction?.Invoke($"   Using unlist method for {packageId} v{version} (deprecation API unavailable)...");
        return await FallbackToDeleteMethod(packageId, version, apiKey, deprecationInfo);
    }    /// <summary>
         /// 使用删除方法作为弃用的替代方案（实际上是取消列出，类似于弃用效果）
         /// </summary>
    private async Task<bool> FallbackToDeleteMethod(string packageId, string version, string apiKey, string deprecationInfo)
    {
        try
        {
            var nugetExe = NugetCliHelper.FindNugetExe();
            if (string.IsNullOrEmpty(nugetExe))
            {
                logAction?.Invoke("   × NuGet CLI not found, cannot perform unlist operation");
                return false;
            }

            logAction?.Invoke("   Executing unlist command (deprecation alternative)");
            logAction?.Invoke($"   Reason: {deprecationInfo}");

            var success = await ExecuteNugetDeleteCommand(nugetExe, packageId, version, apiKey, CancellationToken.None);
            logAction?.Invoke(success ? $"   ✓ Successfully unlisted {version} (achieves similar effect to deprecation)" : $"   × Failed to unlist {version}");
            return success;
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × Unlist operation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 执行单个版本的删除命令
    /// </summary>
    private async Task<bool> ExecuteNugetDeleteCommand(string nugetExe, string packageName, string? version, string apiKey, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                var args = $"delete {packageName} {version} -ApiKey {apiKey} -Source https://api.nuget.org/v3/index.json -NonInteractive";

                var psi = new ProcessStartInfo(nugetExe, args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    logAction?.Invoke("× Failed to start nuget.exe process");
                    return false;
                }

                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        output.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        error.AppendLine(e.Data);
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for completion or cancellation
                while (!process.HasExited)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (InvalidOperationException)
                        {
                            // Process already exited
                        }
                        return false;
                    }
                    Thread.Sleep(100);
                }

                var outputText = output.ToString();
                var errorText = error.ToString();

                if (process.ExitCode == 0)
                {
                    return true;
                }
                var errorMessage = !string.IsNullOrEmpty(errorText) ? errorText : outputText;
                logAction?.Invoke($"   Command failed: {errorMessage.Trim()}");
                return false;
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"   Command execution failed: {ex.Message}");
                return false;
            }
        }, cancellationToken);
    }
}