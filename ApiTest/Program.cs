using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing NuGet API queries for EasilyNET.Core...");

        await TestV3RegistrationDirect();
        await TestV3CatalogPages();
        await TestPackageBaseAddress();

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static async Task TestV3RegistrationDirect()
    {
        try
        {
            Console.WriteLine("\n=== Testing V3 Registration API ===");
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "NugetManager/1.0");

            var url = "https://api.nuget.org/v3/registration5-semver1/easilynet.core/index.json";
            Console.WriteLine($"URL: {url}");

            var response = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);

            var allVersions = new List<(string version, bool listed)>();

            if (doc.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("items", out var inlineItems))
                    {
                        Console.WriteLine($"Found inline items: {inlineItems.GetArrayLength()}");
                        ProcessVersionItems(inlineItems, allVersions);
                    }
                    else if (item.TryGetProperty("@id", out var pageUrl))
                    {
                        Console.WriteLine($"Found page URL: {pageUrl.GetString()}");
                        try
                        {
                            var pageResponse = await http.GetStringAsync(pageUrl.GetString());
                            using var pageDoc = JsonDocument.Parse(pageResponse);

                            if (pageDoc.RootElement.TryGetProperty("items", out var pageItems))
                            {
                                Console.WriteLine($"Page contains: {pageItems.GetArrayLength()} items");
                                ProcessVersionItems(pageItems, allVersions);
                            }
                        }
                        catch (Exception pageEx)
                        {
                            Console.WriteLine($"Error loading page: {pageEx.Message}");
                        }
                    }
                }
            }

            Console.WriteLine($"Total versions found: {allVersions.Count}");
            Console.WriteLine($"Listed: {allVersions.Count(v => v.listed)}, Unlisted: {allVersions.Count(v => !v.listed)}");

            if (allVersions.Count > 0)
            {
                Console.WriteLine("Sample versions:");
                foreach (var (version, listed) in allVersions.Take(10))
                {
                    Console.WriteLine($"  {version} - {(listed ? "Listed" : "Unlisted")}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"V3 Registration API Error: {ex.Message}");
        }
    }

    static void ProcessVersionItems(JsonElement items, List<(string version, bool listed)> result)
    {
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("catalogEntry", out var catalogEntry))
            {
                var version = catalogEntry.TryGetProperty("version", out var versionElement)
                    ? versionElement.GetString() ?? string.Empty
                    : string.Empty;

                var listed = true; // 默认为Listed
                if (catalogEntry.TryGetProperty("listed", out var listedElement))
                {
                    listed = listedElement.GetBoolean();
                }

                if (!string.IsNullOrEmpty(version))
                {
                    result.Add((version, listed));
                }
            }
        }
    }

    static async Task TestV3CatalogPages()
    {
        try
        {
            Console.WriteLine("\n=== Testing V3 Catalog via Service Index ===");
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "NugetManager/1.0");

            // 获取服务索引
            var indexUrl = "https://api.nuget.org/v3/index.json";
            var indexResponse = await http.GetStringAsync(indexUrl);
            using var indexDoc = JsonDocument.Parse(indexResponse);
            string? catalogUrl = null;
            if (indexDoc.RootElement.TryGetProperty("resources", out var resources))
            {
                foreach (var resource in resources.EnumerateArray())
                {
                    if (resource.TryGetProperty("@type", out var type))
                    {
                        var typeStr = type.GetString();
                        if (!string.IsNullOrEmpty(typeStr) && typeStr.Contains("Catalog") && typeStr.Contains("3.0.0"))
                        {
                            catalogUrl = resource.GetProperty("@id").GetString();
                            Console.WriteLine($"Found Catalog URL: {catalogUrl}");
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(catalogUrl))
            {
                // 访问Catalog
                var catalogResponse = await http.GetStringAsync(catalogUrl);
                using var catalogDoc = JsonDocument.Parse(catalogResponse);

                if (catalogDoc.RootElement.TryGetProperty("count", out var count))
                {
                    Console.WriteLine($"Catalog pages count: {count.GetInt32()}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"V3 Catalog Error: {ex.Message}");
        }
    }

    static async Task TestPackageBaseAddress()
    {
        try
        {
            Console.WriteLine("\n=== Testing Package Base Address API ===");
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "NugetManager/1.0");

            // 获取服务索引
            var indexUrl = "https://api.nuget.org/v3/index.json";
            var indexResponse = await http.GetStringAsync(indexUrl);
            using var indexDoc = JsonDocument.Parse(indexResponse);
            string? packageBaseUrl = null;
            if (indexDoc.RootElement.TryGetProperty("resources", out var resources))
            {
                foreach (var resource in resources.EnumerateArray())
                {
                    if (resource.TryGetProperty("@type", out var type))
                    {
                        var typeStr = type.GetString();
                        if (!string.IsNullOrEmpty(typeStr) && typeStr.Contains("PackageBaseAddress"))
                        {
                            packageBaseUrl = resource.GetProperty("@id").GetString();
                            Console.WriteLine($"Found Package Base URL: {packageBaseUrl}");
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(packageBaseUrl))
            {
                // 尝试访问包的版本列表
                var packageUrl = $"{packageBaseUrl.TrimEnd('/')}/easilynet.core/index.json";
                Console.WriteLine($"Package URL: {packageUrl}");

                try
                {
                    var packageResponse = await http.GetStringAsync(packageUrl);
                    using var packageDoc = JsonDocument.Parse(packageResponse);

                    if (packageDoc.RootElement.TryGetProperty("versions", out var versions))
                    {
                        Console.WriteLine($"Package versions count: {versions.GetArrayLength()}");

                        Console.WriteLine("Sample versions from Package Base Address:");
                        var versionList = versions.EnumerateArray().Take(10).ToList();
                        foreach (var version in versionList)
                        {
                            Console.WriteLine($"  {version.GetString()}");
                        }
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"Package Base Address HTTP Error: {httpEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Package Base Address Error: {ex.Message}");
        }
    }
}
