using System.Text.Json;

namespace NugetManager.Services;

/// <summary>
/// 处理NuGet API相关的查询功能 - 基于PowerShell脚本优化的实现
/// </summary>
public sealed class NugetApiService(Action<string>? logAction = null)
{
    private const string DefaultSource = "https://api.nuget.org/v3/index.json";
    private const int DefaultTimeoutSeconds = 30;
    private const int DefaultRetryCount = 3;

    /// <summary>
    /// 获取NuGet服务索引并解析注册服务URL
    /// </summary>
    public async Task<string?> GetRegistrationServiceUrlAsync(string source = DefaultSource)
    {
        try
        {
            logAction?.Invoke($"📋 Resolving registration service URL for {source}...");

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds) };
            var response = await http.GetStringAsync(source);
            var json = JsonDocument.Parse(response);

            if (!json.RootElement.TryGetProperty("resources", out var resources))
            {
                logAction?.Invoke("   × Cannot find resources node");
                return null;
            }

            foreach (var resource in resources.EnumerateArray())
            {
                if (resource.TryGetProperty("@type", out var typeElement))
                {
                    var type = typeElement.GetString();
                    if (type != null && type.StartsWith("RegistrationsBaseUrl"))
                    {
                        if (resource.TryGetProperty("@id", out var idElement))
                        {
                            var url = idElement.GetString()?.TrimEnd('/');
                            logAction?.Invoke($"   ✓ Registration service URL: {url}");
                            return url;
                        }
                    }
                }
            }

            logAction?.Invoke("   × Cannot find RegistrationsBaseUrl resource");
            return null;
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × Cannot access {source}, error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 使用注册API获取包的所有版本（基于PowerShell脚本的逻辑）
    /// </summary>
    public async Task<List<(string Version, bool Listed)>> GetPackageVersionsAsync(string packageName, string source = DefaultSource)
    {
        var result = new List<(string Version, bool Listed)>();

        try
        {
            logAction?.Invoke($"📦 Getting version information for package {packageName}...");

            // 首先获取注册服务URL
            var registrationService = await GetRegistrationServiceUrlAsync(source);
            if (string.IsNullOrEmpty(registrationService))
            {
                logAction?.Invoke("   × Cannot get registration service URL, using fallback method");
                await UseV3RegistrationFallback(packageName, result);
                return result;
            }

            // 使用动态解析的注册服务URL
            await ProcessRegistrationWithPagination(registrationService, packageName, result);

            logAction?.Invoke($"   ✓ Total {result.Count} versions found");
            return result;
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × Failed to get package versions: {ex.Message}");
            // 尝试回退方法
            await UseV3RegistrationFallback(packageName, result);
            return result;
        }
    }

    /// <summary>
    /// 处理带分页的注册API响应（基于PowerShell脚本逻辑）
    /// </summary>
    private async Task ProcessRegistrationWithPagination(string registrationService, string packageName, List<(string Version, bool Listed)> result)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds) };
        var baseUrl = $"{registrationService}/{packageName.ToLower()}/index.json";
        var visitedUrls = new HashSet<string>();
        var currentUrl = baseUrl;

        while (!string.IsNullOrEmpty(currentUrl) && !visitedUrls.Contains(currentUrl))
        {
            visitedUrls.Add(currentUrl);
            logAction?.Invoke($"   Querying URL: {currentUrl}");

            try
            {
                var response = await http.GetStringAsync(currentUrl);
                var json = JsonDocument.Parse(response);

                if (json.RootElement.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        // 处理内联版本项
                        if (item.TryGetProperty("items", out var inlineItems))
                        {
                            ProcessInlineItems(inlineItems, result);
                        }
                        // 处理只有catalogEntry的项
                        else if (item.TryGetProperty("catalogEntry", out var catalogEntry))
                        {
                            ProcessCatalogEntry(catalogEntry, result);
                        }
                        // 处理需要进一步请求的项（分页）
                        else if (item.TryGetProperty("@id", out var itemIdElement))
                        {
                            var itemUrl = itemIdElement.GetString();
                            if (!string.IsNullOrEmpty(itemUrl) && !visitedUrls.Contains(itemUrl))
                            {
                                await ProcessRegistrationPage(http, itemUrl, result, visitedUrls);
                            }
                        }
                    }
                }

                // 检查是否有下一页
                currentUrl = null;
                if (json.RootElement.TryGetProperty("items", out var itemsForNext) && itemsForNext.GetArrayLength() > 0)
                {
                    var lastItem = itemsForNext.EnumerateArray().Last();
                    if (lastItem.TryGetProperty("@id", out var nextIdElement))
                    {
                        var nextUrl = nextIdElement.GetString();
                        if (!string.IsNullOrEmpty(nextUrl) && !visitedUrls.Contains(nextUrl) && nextUrl != currentUrl)
                        {
                            currentUrl = nextUrl;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"   × Failed to process URL {currentUrl}: {ex.Message}");
                break;
            }
        }

        // 去重并排序
        var uniqueVersions = result
            .GroupBy(x => x.Version, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderByDescending(x => x.Version)
            .ToList();

        result.Clear();
        result.AddRange(uniqueVersions);
    }

    /// <summary>
    /// 处理注册API的分页响应
    /// </summary>
    private async Task ProcessRegistrationPage(HttpClient http, string pageUrl, List<(string Version, bool Listed)> result, HashSet<string> visitedUrls)
    {
        if (visitedUrls.Contains(pageUrl)) return;

        visitedUrls.Add(pageUrl);

        try
        {
            logAction?.Invoke($"   Processing page: {pageUrl}");
            var response = await http.GetStringAsync(pageUrl);
            var json = JsonDocument.Parse(response);

            if (json.RootElement.TryGetProperty("items", out var items))
            {
                ProcessInlineItems(items, result);
            }
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × Failed to process page {pageUrl}: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理内联版本项
    /// </summary>
    private void ProcessInlineItems(JsonElement items, List<(string Version, bool Listed)> result)
    {
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("catalogEntry", out var catalogEntry))
            {
                ProcessCatalogEntry(catalogEntry, result);
            }
        }
    }

    /// <summary>
    /// 处理单个catalogEntry
    /// </summary>
    private void ProcessCatalogEntry(JsonElement catalogEntry, List<(string Version, bool Listed)> result)
    {
        if (!catalogEntry.TryGetProperty("version", out var versionElement)) return;

        var version = versionElement.GetString();
        if (string.IsNullOrEmpty(version)) return;

        // 检查listed状态，默认为true
        var listed = true;
        if (catalogEntry.TryGetProperty("listed", out var listedElement))
        {
            listed = listedElement.GetBoolean();
        }

        result.Add((version, listed));
    }

    /// <summary>
    /// V3注册API回退方法
    /// </summary>
    private async Task UseV3RegistrationFallback(string packageName, List<(string Version, bool Listed)> result)
    {
        try
        {
            logAction?.Invoke("🔄 Using V3 registration API fallback method...");
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds) };
            var registrationUrl = $"https://api.nuget.org/v3/registration5-semver1/{packageName.ToLowerInvariant()}/index.json";

            logAction?.Invoke($"   GET {registrationUrl}");
            var response = await http.GetStringAsync(registrationUrl);
            var json = JsonDocument.Parse(response);

            if (json.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("items", out var packageItems))
                    {
                        ProcessInlineItems(packageItems, result);
                    }
                    else if (item.TryGetProperty("catalogEntry", out var catalogEntry))
                    {
                        ProcessCatalogEntry(catalogEntry, result);
                    }
                }
                logAction?.Invoke($"   ✓ Fallback method found {result.Count} versions");
            }
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × V3 registration API fallback method failed: {ex.Message}");
        }
    }
}
