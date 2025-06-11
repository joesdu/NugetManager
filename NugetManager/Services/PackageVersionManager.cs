namespace NugetManager.Services;

/// <summary>
/// 管理包版本查询和状态校准的服务
/// </summary>
/// <remarks>
/// 初始化PackageVersionManager实例
/// </remarks>
/// <param name="logAction">日志记录回调方法</param>
public sealed class PackageVersionManager(Action<string>? logAction = null)
{
    private readonly NugetApiService _apiService = new(logAction);
    private readonly WebScrapingService _webScrapingService = new(logAction); 
    
    /// <summary>
    /// 查询包的所有版本及其Listed状态
    /// </summary>
    public async Task<List<(string Version, bool Listed)>> QueryAllVersionsWithStatusAsync(string packageName, int querySource = 0)
    {
        var result = new List<(string Version, bool Listed)>();
        try
        {
            // 根据查询源执行不同的查询策略
            result = await ExecuteQueryStrategy(packageName, querySource);
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"× Query failed: {ex.Message}");
            throw new InvalidOperationException($"Failed to query package versions: {ex.Message}", ex);
        }

        logAction?.Invoke($"✓ Found {result.Count} versions total");
        return result;
    }

    /// <summary>
    /// 根据查询源选择执行不同的查询策略
    /// </summary>
    private async Task<List<(string Version, bool Listed)>> ExecuteQueryStrategy(string packageName, int querySource)
    {
        switch (querySource)
        {
            case 0: // 新的增强型注册API（推荐，基于PowerShell脚本优化）
                return await _apiService.GetPackageVersionsAsync(packageName);
            case 1: // Web爬虫方式（回退选项）
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                http.Timeout = TimeSpan.FromSeconds(30);
                var webResult = new List<(string Version, bool Listed)>();
                await _webScrapingService.UseWebScrapingStrategy(http, packageName, webResult);
                return webResult;
            default:
                // 默认使用增强型注册API
                return await _apiService.GetPackageVersionsAsync(packageName);
        }
    }
}