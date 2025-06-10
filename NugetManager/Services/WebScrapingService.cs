using System.Text.RegularExpressions;

namespace NugetManager.Services;

/// <summary>
/// 处理NuGet网页抓取相关的功能
/// </summary>
public partial class WebScrapingService(Action<string>? logAction = null)
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
            await TryParseVersionHistoryPage(http, packageName, result);
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
            var content = await http.GetStringAsync(url);

            // 查找版本历史表格
            const string tablePattern = """<table[^>]*class="[^"]*version[^"]*"[^>]*>(.*?)</table>""";
            var tableMatch = Regex.Match(content, tablePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (tableMatch.Success)
            {
                var tableContent = tableMatch.Groups[1].Value;
                var rowMatches = TableRowPattern().Matches(tableContent);

                foreach (Match rowMatch in rowMatches)
                {
                    var rowContent = rowMatch.Value;

                    // 提取版本号
                    var versionMatch = VersionLinkPattern().Match(rowContent);
                    if (!versionMatch.Success) continue;
                    var version = versionMatch.Groups[1].Value.Trim();
                    if (!VersionFormatPattern().IsMatch(version)) continue;
                    // 检查是否有管理链接（只有Listed版本才有）
                    var hasManageLink = ManageLinkPattern().IsMatch(rowContent);
                    var listed = hasManageLink || !rowContent.Contains("unlisted", StringComparison.OrdinalIgnoreCase);

                    result.Add((version, listed));
                    logAction?.Invoke($"   Found: {version} (Listed: {listed})");
                }
            }

            // 尝试从页面的JavaScript数据中提取版本信息
            TryExtractFromPageScript(content);
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × nuget.org web scraping failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 尝试解析版本历史页面
    /// </summary>
    private async Task TryParseVersionHistoryPage(HttpClient http, string packageName, List<(string Version, bool Listed)> result)
    {
        try
        {
            logAction?.Invoke("   Trying version history page...");
            var historyUrl = $"https://www.nuget.org/packages/{packageName}/versions";
            var content = await http.GetStringAsync(historyUrl);

            // 解析版本历史页面的表格
            var versionRows = Regex.Matches(content, @"<tr[^>]*>.*?<td[^>]*>.*?(\d+\.\d+[^<]*)</td>.*?</tr>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match row in versionRows)
            {
                var version = row.Groups[1].Value.Trim();
                if (!VersionFormatPattern().IsMatch(version)) continue;
                // 检查该行是否包含unlisted指示器
                var isUnlisted = row.Value.Contains("unlisted", StringComparison.OrdinalIgnoreCase) ||
                                 row.Value.Contains("delisted", StringComparison.OrdinalIgnoreCase);

                result.Add((version, !isUnlisted));
            }
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × Version history page parsing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 尝试从页面脚本中提取版本信息
    /// </summary>
    private void TryExtractFromPageScript(string content)
    {
        try
        {
            // 查找页面中的JSON数据
            const string scriptPattern = @"window\.nuget\s*=\s*({.*?});";
            var scriptMatch = Regex.Match(content, scriptPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (scriptMatch.Success)
            {
                var jsonContent = scriptMatch.Groups[1].Value;
                // 这里可以进一步解析JSON数据来获取版本信息
                // 由于JSON结构可能复杂，暂时跳过具体实现
            }
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"   × Script extraction failed: {ex.Message}");
        }
    }
}
