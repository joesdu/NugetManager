using System.Text.RegularExpressions;

namespace NugetManager.Services;

/// <summary>
/// 处理NuGet网页抓取相关的功能
/// </summary>
public sealed partial class WebScrapingService(Action<string>? logAction = null)
{
    // Generated Regex methods for better performance
    [GeneratedRegex("<tr[^>]*>.*?</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TableRowPattern();

    [GeneratedRegex("""<a[^>]*href="/packages/[^/]+/([^/"]+)"[^>]*>([^<]+)</a>""", RegexOptions.IgnoreCase)]
    private static partial Regex VersionLinkPattern();

    [GeneratedRegex("""<a[^>]*href="[^"]*?/Manage[^"]*"[^>]*>([^<]*)</a>""", RegexOptions.IgnoreCase)]
    private static partial Regex ManageLinkPattern();

    [GeneratedRegex(@"^[\d]+(\.[^\s/""<>]+)*$")]
    private static partial Regex VersionFormatPattern();

    /// <summary>
    /// 使用网页抓取策略查询版本
    /// </summary>
    public async Task UseWebScrapingStrategy(HttpClient http, string packageName, List<(string Version, bool Listed)> result)
    {
        try
        {
            logAction?.Invoke("🌐 Trying Web Scraping Strategy...");
            await TryWebScrapingFromNugetOrg(http, packageName, result);
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × Web Scraping Strategy failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 尝试从nuget.org抓取版本信息
    /// </summary>
    private async Task TryWebScrapingFromNugetOrg(HttpClient http, string packageName, List<(string Version, bool Listed)> result)
    {
        try
        {
            logAction?.Invoke("   Trying nuget.org web scraping...");
            var url = $"https://www.nuget.org/packages/{packageName}";
            logAction?.Invoke($"   GET {url}");
            var content = await http.GetStringAsync(url);

            // 新版 nuget.org: 解析 #versions-tab 下的版本表格
            const string versionsTabPattern = """<div[^>]+id=\"versions-tab\"[\s\S]*?<table[\s\S]*?</table>""";
            var versionsTabMatch = Regex.Match(content, versionsTabPattern, RegexOptions.IgnoreCase);
            if (versionsTabMatch.Success)
            {
                var tableHtml = versionsTabMatch.Value;
                var rowMatches = Regex.Matches(tableHtml, "<tr[^>]*>.*?</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match rowMatch in rowMatches)
                {
                    var rowContent = rowMatch.Value;
                    // 匹配 <a href="/packages/{packageName}/{version}" ...>version</a>
                    var versionMatch = Regex.Match(rowContent, $"<a[^>]*href=\"/packages/{Regex.Escape(packageName)}/([^\"/]+)\"[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase);
                    if (!versionMatch.Success) continue;
                    var version = versionMatch.Groups[1].Value.Trim();
                    if (string.IsNullOrWhiteSpace(version)) continue;
                    result.Add((version, true)); // #versions-tab 只展示 listed 版本
                    logAction?.Invoke($"   Found: {version} (Listed: True)");
                }
            }

            // 兼容旧版表格（极少数情况）
            const string tablePattern = """<table[^>]*class=\"[^\"]*version[^\"]*\"[^>]*>(.*?)</table>""";
            var tableMatch = Regex.Match(content, tablePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (tableMatch.Success)
            {
                var tableContent = tableMatch.Groups[1].Value;
                var rowMatches = TableRowPattern().Matches(tableContent);
                foreach (Match rowMatch in rowMatches)
                {
                    var rowContent = rowMatch.Value;
                    var versionMatch = VersionLinkPattern().Match(rowContent);
                    if (!versionMatch.Success) continue;
                    var version = versionMatch.Groups[1].Value.Trim();
                    if (!VersionFormatPattern().IsMatch(version)) continue;
                    var hasManageLink = ManageLinkPattern().IsMatch(rowContent);
                    var listed = hasManageLink || !rowContent.Contains("unlisted", StringComparison.OrdinalIgnoreCase);
                    result.Add((version, listed));
                    logAction?.Invoke($"   Found: {version} (Listed: {listed})");
                }
            }
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × nuget.org web scraping failed: {ex.Message}");
        }
    }
}