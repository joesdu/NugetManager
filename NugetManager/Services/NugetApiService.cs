using System.Text.Json;

namespace NugetManager.Services;

/// <summary>
/// 处理NuGet API相关的查询功能
/// </summary>
public sealed class NugetApiService(Action<string>? logAction = null)
{

    /// <summary>
    /// 使用Package Base Address API策略查询版本
    /// </summary>
    public async Task UsePackageBaseAddressStrategy(HttpClient http, string packageName, List<(string Version, bool Listed)> result)
    {
        try
        {
            logAction?.Invoke("📦 Trying Package Base Address API...");
            const string baseUrl = "https://api.nuget.org/v3-flatcontainer";
            var indexUrl = $"{baseUrl}/{packageName.ToLowerInvariant()}/index.json";

            logAction?.Invoke($"   GET {indexUrl}");
            var response = await http.GetStringAsync(indexUrl);
            var json = JsonDocument.Parse(response);

            if (json.RootElement.TryGetProperty("versions", out var versions))
            {
                result.AddRange(from version in versions.EnumerateArray() select version.GetString() into versionStr where !string.IsNullOrEmpty(versionStr) select (versionStr, true));
                logAction?.Invoke($"   ✓ Found {result.Count} Listed versions via Package Base Address API");
            }
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × Package Base Address API failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 使用V3 Registration API策略查询版本
    /// </summary>
    public async Task UseV3RegistrationStrategy(HttpClient http, string packageName, List<(string Version, bool Listed)> result)
    {
        try
        {
            logAction?.Invoke("🔍 Trying Enhanced V3 Registration API...");
            var registrationUrl = $"https://api.nuget.org/v3/registration5-semver1/{packageName.ToLowerInvariant()}/index.json";

            logAction?.Invoke($"   GET {registrationUrl}");
            var response = await http.GetStringAsync(registrationUrl);
            var json = JsonDocument.Parse(response);

            if (json.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (!item.TryGetProperty("items", out var packageItems)) continue;
                    foreach (var packageItem in packageItems.EnumerateArray())
                    {
                        if (!packageItem.TryGetProperty("catalogEntry", out var catalogEntry)) continue;
                        if (!catalogEntry.TryGetProperty("version", out var versionProp)) continue;
                        var version = versionProp.GetString();
                        if (string.IsNullOrEmpty(version)) continue;
                        // 检查是否listed
                        var isListed = true;
                        if (catalogEntry.TryGetProperty("listed", out var listedProp))
                        {
                            isListed = listedProp.GetBoolean();
                        }

                        result.Add((version, isListed));
                    }
                }
                logAction?.Invoke($"   ✓ Found {result.Count} versions via V3 Registration API");
            }
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × V3 Registration API failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 使用综合API搜索策略
    /// </summary>
    public async Task UseComprehensiveApiSearch(HttpClient http, string packageName, List<(string Version, bool Listed)> result)
    {
        logAction?.Invoke("🔄 Trying Comprehensive API Search...");

        // 依次尝试多个API策略
        await UsePackageBaseAddressStrategy(http, packageName, result);
        await UseV3RegistrationStrategy(http, packageName, result);

        // 去重
        var uniqueResults = result
            .GroupBy(x => x.Version, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        result.Clear();
        result.AddRange(uniqueResults);

        logAction?.Invoke($"   ✓ Comprehensive search completed: {result.Count} unique versions");
    }

    /// <summary>
    /// 使用Registration API丰富状态信息
    /// </summary>
    public async Task EnrichWithRegistrationApiStatus(HttpClient http, string packageName, List<(string Version, bool Listed)> result)
    {
        try
        {
            var registrationUrl = $"https://api.nuget.org/v3/registration5-semver1/{packageName.ToLowerInvariant()}/index.json";
            var response = await http.GetStringAsync(registrationUrl);
            using var doc = JsonDocument.Parse(response);

            var statusDict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (doc.RootElement.TryGetProperty("items", out var items))
            {
                ProcessRegistrationItems(items, statusDict);
            }

            // 更新结果中的状态信息
            for (var i = 0; i < result.Count; i++)
            {
                var (version, currentListed) = result[i];
                if (!statusDict.TryGetValue(version, out var registrationListed)) continue;
                // 如果Registration API给出了不同的状态，以Registration API为准
                if (currentListed != registrationListed)
                {
                    result[i] = (version, registrationListed);
                }
            }
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × Registration API status enrichment failed: {ex.Message}");
        }
    }


    private static void ProcessRegistrationItems(JsonElement items, Dictionary<string, bool> statusDict)
    {
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("catalogEntry", out var catalogEntry)) continue;
            var version = catalogEntry.TryGetProperty("version", out var versionElement)
                ? versionElement.GetString() ?? string.Empty
                : string.Empty;

            var listed = true; // 默认为Listed
            if (catalogEntry.TryGetProperty("listed", out var listedElement)) listed = listedElement.GetBoolean();

            if (!string.IsNullOrEmpty(version)) statusDict[version] = listed;
        }
    }
}
