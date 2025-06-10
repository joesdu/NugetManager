namespace NugetManager.Services;

/// <summary>
/// 管理包版本查询和状态校准的服务
/// </summary>
/// <remarks>
/// 初始化PackageVersionManager实例
/// </remarks>
/// <param name="logAction">日志记录回调方法</param>
public class PackageVersionManager(Action<string>? logAction = null)
{
    private readonly NugetApiService _apiService = new(logAction);
    private readonly NugetCliService _cliService = new(logAction);
    private readonly WebScrapingService _webScrapingService = new(logAction);

    /// <summary>
    /// 查询包的所有版本及其Listed状态
    /// </summary>
    public async Task<List<(string Version, bool Listed)>> QueryAllVersionsWithStatusAsync(string packageName, int querySource = 0)
    {
        var result = new List<(string Version, bool Listed)>();
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0");
            http.Timeout = TimeSpan.FromSeconds(30);

            // 根据查询源执行不同的查询策略
            await ExecuteQueryStrategy(http, packageName, result, querySource);
            //// 状态校准
            //await QueryWithStatusCalibration(http, packageName, result);
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"× Query failed: {ex.Message}");
            throw new InvalidOperationException($"Failed to query package versions: {ex.Message}", ex);
        }

        // 去重并排序
        var uniqueResults = result
                            .GroupBy(x => x.Version, StringComparer.OrdinalIgnoreCase)
                            .Select(g => g.First())
                            .OrderByDescending(x => x.Version, StringComparer.OrdinalIgnoreCase)
                            .ToList();
        logAction?.Invoke($"✓ Found {uniqueResults.Count} versions total");
        return uniqueResults;
    }

    /// <summary>
    /// 根据查询源选择执行不同的查询策略
    /// </summary>
    private async Task ExecuteQueryStrategy(HttpClient http, string packageName, List<(string Version, bool Listed)> result, int querySource)
    {
        switch (querySource)
        {
            case 0: // Package Base Address API (Recommended)
                await _apiService.UsePackageBaseAddressStrategy(http, packageName, result);
                break;
            case 1: // Enhanced V3 Registration API
                await _apiService.UseV3RegistrationStrategy(http, packageName, result);
                break;
            case 2: // NuGet CLI Tool
                await _cliService.UseNuGetCliStrategy(packageName, result);
                break;
            case 3: // Web Scraping (nuget.org)
                await _webScrapingService.UseWebScrapingStrategy(http, packageName, result);
                break;
            case 4: // Comprehensive API Search (All Sources)
                await _apiService.UseComprehensiveApiSearch(http, packageName, result);
                break;
            default:
                await _apiService.UsePackageBaseAddressStrategy(http, packageName, result);
                break;
        }
    }
}