using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Features.Management.Components;

public static class ReportChartOptionsFactory
{
    private const string Ink = "#313647";
    private const string Charcoal = "#45474B";
    private const string Blue = "#435663";
    private const string Green = "#2F5249";
    private const string Sage = "#97B067";
    private const string Gold = "#F4CE14";

    public static string GetChartHeight(int count) => count switch
    {
        <= 4 => "220px",
        <= 8 => "260px",
        <= 15 => "320px",
        _ => "380px"
    };

    public static string GetQuartileChartHeight(ArticlesQuartileStatsDto? stats)
    {
        if (stats == null)
        {
            return "260px";
        }

        var total = stats.Q1Count + stats.Q2Count + stats.Q3Count + stats.Q4Count + stats.NoQuartileCount;

        return total switch
        {
            <= 0 => "260px",
            <= 20 => "280px",
            <= 50 => "320px",
            _ => "360px"
        };
    }

    public static object? BuildVerticalBar(string[]? categories, int[]? data, string seriesName, string color = Blue, bool rotateLabels = false)
    {
        if (categories == null || data == null || categories.Length == 0)
        {
            return null;
        }

        return new
        {
            color = new[] { color },
            tooltip = new { trigger = "axis" },
            toolbox = BuildToolbox(),
            grid = new { left = "8%", right = "5%", bottom = rotateLabels ? "18%" : "10%", top = "16%" },
            xAxis = new
            {
                type = "category",
                data = categories,
                axisLabel = rotateLabels
                    ? (object)new { rotate = 30, interval = 0, color = Charcoal }
                    : new { interval = 0, color = Charcoal } as object
            },
            yAxis = new
            {
                type = "value",
                axisLabel = new { color = Charcoal },
                splitLine = new { lineStyle = new { color = "rgba(67,86,99,0.12)" } }
            },
            series = new object[]
            {
                new
                {
                    name = seriesName,
                    type = "bar",
                    data,
                    barMaxWidth = 38,
                    itemStyle = new { color, borderRadius = new[] { 8, 8, 0, 0 } }
                }
            }
        };
    }

    public static object? BuildHorizontalBar(string[]? categories, int[]? data, string color = Green)
    {
        if (categories == null || data == null || categories.Length == 0)
        {
            return null;
        }

        return new
        {
            color = new[] { color },
            tooltip = new { trigger = "axis", axisPointer = new { type = "shadow" } },
            toolbox = BuildToolbox(),
            grid = new { left = "25%", right = "5%", bottom = "5%", top = "14%" },
            xAxis = new
            {
                type = "value",
                axisLabel = new { color = Charcoal },
                splitLine = new { lineStyle = new { color = "rgba(67,86,99,0.12)" } }
            },
            yAxis = new
            {
                type = "category",
                data = categories,
                axisLabel = new { interval = 0, color = Charcoal }
            },
            series = new object[]
            {
                new
                {
                    name = "Artículos",
                    type = "bar",
                    data,
                    barMaxWidth = 28,
                    itemStyle = new { color, borderRadius = new[] { 0, 8, 8, 0 } }
                }
            }
        };
    }

    public static object? BuildQuartileDonut(ArticlesQuartileStatsDto? stats)
    {
        if (stats is null)
        {
            return null;
        }

        var total = stats.Q1Count + stats.Q2Count + stats.Q3Count + stats.Q4Count + stats.NoQuartileCount;
        if (total <= 0)
        {
            return null;
        }

        var data = new List<Dictionary<string, object>>
        {
            new() { ["value"] = stats.Q1Count, ["name"] = "Q1" },
            new() { ["value"] = stats.Q2Count, ["name"] = "Q2" },
            new() { ["value"] = stats.Q3Count, ["name"] = "Q3" },
            new() { ["value"] = stats.Q4Count, ["name"] = "Q4" },
            new() { ["value"] = stats.NoQuartileCount, ["name"] = "Sin cuartil" }
        };

        return new
        {
            color = new[] { Green, Sage, Gold, Blue, "#A3B087" },
            tooltip = new { trigger = "item", formatter = "{b}: {c} artículos ({d}%)" },
            legend = new { orient = "vertical", left = "left", textStyle = new { color = Charcoal } },
            series = new object[]
            {
                new
                {
                    name = "Distribución por cuartil",
                    type = "pie",
                    radius = new[] { "45%", "70%" },
                    avoidLabelOverlap = true,
                    center = new[] { "55%", "55%" },
                    data,
                    label = new { show = true, formatter = "{b}: {d}%", color = Ink },
                    labelLine = new { show = true }
                }
            }
        };
    }

    public static object? BuildTrend(IReadOnlyList<ArticlesByYearDto>? byYear)
    {
        if (byYear == null || byYear.Count == 0)
        {
            return null;
        }

        var years = byYear.Select(x => x.Year.ToString()).ToArray();
        var counts = byYear.Select(x => x.Count).ToArray();

        return new
        {
            color = new[] { Green },
            tooltip = new { trigger = "axis" },
            toolbox = BuildToolbox(),
            grid = new { left = "8%", right = "5%", bottom = "10%", top = "16%" },
            xAxis = new
            {
                type = "category",
                data = years,
                axisLabel = new { color = Charcoal }
            },
            yAxis = new
            {
                type = "value",
                axisLabel = new { color = Charcoal },
                splitLine = new { lineStyle = new { color = "rgba(67,86,99,0.12)" } }
            },
            series = new object[]
            {
                new
                {
                    name = "Artículos",
                    type = "line",
                    smooth = true,
                    data = counts,
                    itemStyle = new { color = Green },
                    lineStyle = new { color = Green, width = 3 },
                    areaStyle = new { color = "rgba(47,82,73,0.12)" }
                }
            }
        };
    }

    public static object? BuildSummaryDonut(IEnumerable<ReportingSummaryItemDto>? items, string title = "Distribución")
    {
        var data = items?
            .Where(x => x.TotalArticles > 0)
            .Take(8)
            .Select(x => new Dictionary<string, object>
            {
                ["value"] = x.TotalArticles,
                ["name"] = x.Name
            })
            .ToList();

        if (data is null || data.Count == 0)
        {
            return null;
        }

        return new
        {
            color = new[] { Green, Sage, Gold, Blue, "#A3B087", "#E3DE61", Ink, "#437057" },
            tooltip = new { trigger = "item", formatter = "{b}: {c} artículos ({d}%)" },
            legend = new { orient = "vertical", left = "left", top = "middle", textStyle = new { color = Charcoal } },
            series = new object[]
            {
                new
                {
                    name = title,
                    type = "pie",
                    radius = new[] { "44%", "70%" },
                    center = new[] { "62%", "52%" },
                    avoidLabelOverlap = true,
                    data,
                    label = new { show = true, formatter = "{d}%", color = Ink },
                    labelLine = new { show = true }
                }
            }
        };
    }

    public static object? BuildAuthorRoleDonut(int primaryAuthorLinks, int coauthorLinks)
    {
        if (primaryAuthorLinks + coauthorLinks <= 0)
        {
            return null;
        }

        var data = new List<Dictionary<string, object>>
        {
            new() { ["value"] = primaryAuthorLinks, ["name"] = "Autoría principal" },
            new() { ["value"] = coauthorLinks, ["name"] = "Coautoría" }
        };

        return new
        {
            color = new[] { Green, Gold },
            tooltip = new { trigger = "item", formatter = "{b}: {c} participaciones ({d}%)" },
            legend = new { bottom = "0", textStyle = new { color = Charcoal } },
            series = new object[]
            {
                new
                {
                    name = "Participación",
                    type = "pie",
                    radius = new[] { "45%", "72%" },
                    center = new[] { "50%", "45%" },
                    data,
                    label = new { show = true, formatter = "{b}: {d}%", color = Ink }
                }
            }
        };
    }

    private static object BuildToolbox()
        => new
        {
            show = true,
            orient = "horizontal",
            right = "8",
            top = "8",
            feature = new
            {
                saveAsImage = new
                {
                    show = true,
                    title = "Descargar PNG",
                    pixelRatio = 2
                }
            }
        };
}
