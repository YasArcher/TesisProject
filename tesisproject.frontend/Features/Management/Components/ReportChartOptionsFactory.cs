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
    private const string SoftSage = "#A3B087";
    private const string CreamGold = "#E3DE61";
    private const string DeepRed = "#7A1E19";
    private const string Forest = "#437057";
    private const string Slate = "#5E6B75";
    private const string Amber = "#CD982E";
    private const string Mist = "#C9D4D9";
    private const string DeepTeal = "#003638";
    private const string Teal = "#055052";
    private const string ForestTeal = "#285A48";
    private const string Mint = "#408A71";
    private const string Olive = "#628141";
    private const string WineDeep = "#3E0703";
    private const string Wine = "#660B05";
    private const string Redwood = "#8C1007";

    private static readonly string[] ExecutivePalette = [Green, Blue, Gold, Sage, Forest, Slate, Amber, DeepRed];
    private static readonly string[] BarPalette = [DeepTeal, Teal, ForestTeal, Mint, Olive, Wine, Redwood, WineDeep];
    private static readonly string[] DonutPalette = [DeepTeal, Teal, ForestTeal, Mint, Olive, Wine, Redwood, WineDeep];

    public static string GetChartHeight(int count) => count switch
    {
        <= 4 => "240px",
        <= 8 => "280px",
        <= 15 => "340px",
        <= 20 => "520px",
        _ => "560px"
    };

    public static string GetCompactChartHeight(int count) => count switch
    {
        <= 4 => "220px",
        <= 8 => "250px",
        <= 15 => "280px",
        <= 20 => "320px",
        _ => "360px"
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

    public static object? BuildHorizontalBar(
        string[]? categories,
        int[]? data,
        string color = Green,
        bool usePalette = false,
        bool showEndLabels = false)
    {
        if (categories == null || data == null || categories.Length == 0)
        {
            return null;
        }

        var seriesData = usePalette
            ? data.Select((value, index) => new
            {
                value,
                itemStyle = new
                {
                    color = BarPalette[index % BarPalette.Length],
                    borderRadius = new[] { 0, 8, 8, 0 }
                }
            }).ToArray() as object
            : data;

        return new
        {
            color = usePalette ? BarPalette : new[] { color },
            tooltip = new { trigger = "axis", axisPointer = new { type = "shadow" } },
            grid = new { left = "34%", right = showEndLabels ? "12%" : "6%", bottom = "5%", top = "9%" },
            xAxis = new
            {
                type = "value",
                axisLabel = new { color = Charcoal },
                splitLine = new { lineStyle = new { color = "rgba(0,54,56,0.10)", type = "dashed" } }
            },
            yAxis = new
            {
                type = "category",
                data = categories,
                axisTick = new { show = false },
                axisLine = new { lineStyle = new { color = "rgba(0,54,56,0.16)" } },
                axisLabel = new { interval = 0, color = Charcoal, fontSize = 11, fontWeight = 700 }
            },
            series = new object[]
            {
                new
                {
                    name = "Artículos",
                    type = "bar",
                    data = seriesData,
                    barMaxWidth = 18,
                    itemStyle = new { color, borderRadius = new[] { 0, 8, 8, 0 } },
                    label = new
                    {
                        show = showEndLabels,
                        position = "right",
                        color = Ink,
                        fontWeight = 900,
                        formatter = "{c}"
                    }
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
            color = DonutPalette,
            tooltip = new { trigger = "item", formatter = "{b}: {c} artículos ({d}%)" },
            legend = new
            {
                orient = "vertical",
                left = "left",
                top = "middle",
                itemWidth = 10,
                itemHeight = 10,
                textStyle = new { color = Charcoal, fontSize = 12, fontWeight = 700 }
            },
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
                    itemStyle = new { borderColor = "#ffffff", borderWidth = 3 },
                    emphasis = new { scale = true, scaleSize = 6 },
                    label = new { show = true, formatter = "{b}\n{d}%", color = Ink, fontWeight = 700, fontSize = 11 },
                    labelLine = new { show = true, length = 10, length2 = 8 }
                }
            }
        };
    }

    public static object? BuildTrend(IReadOnlyList<ArticlesByYearDto>? byYear, bool compact = false)
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
            tooltip = new { trigger = "axis", axisPointer = new { type = "line" } },
            toolbox = BuildToolbox(),
            grid = compact
                ? new { left = "7%", right = "4%", bottom = "12%", top = "12%" }
                : new { left = "8%", right = "5%", bottom = "10%", top = "16%" },
            xAxis = new
            {
                type = "category",
                data = years,
                boundaryGap = false,
                axisTick = new { show = false },
                axisLine = new { lineStyle = new { color = "rgba(0,54,56,0.22)" } },
                axisLabel = new { color = Charcoal, fontWeight = 700 }
            },
            yAxis = new
            {
                type = "value",
                axisLabel = new { color = Charcoal },
                splitLine = new { lineStyle = new { color = "rgba(0,54,56,0.10)", type = "dashed" } }
            },
            series = new object[]
            {
                new
                {
                    name = "Artículos",
                    type = "line",
                    smooth = true,
                    symbol = "circle",
                    symbolSize = compact ? 7 : 8,
                    data = counts,
                    itemStyle = new { color = Teal, borderColor = "#ffffff", borderWidth = 2 },
                    lineStyle = new { color = Teal, width = compact ? 3 : 4 },
                    areaStyle = new
                    {
                        color = new
                        {
                            type = "linear",
                            x = 0,
                            y = 0,
                            x2 = 0,
                            y2 = 1,
                            colorStops = new object[]
                            {
                                new { offset = 0, color = "rgba(64,138,113,0.26)" },
                                new { offset = 1, color = "rgba(0,54,56,0.02)" }
                            }
                        }
                    }
                }
            }
        };
    }

    public static object? BuildSummaryDonut(
        IEnumerable<ReportingSummaryItemDto>? items,
        string title = "Distribución",
        bool compact = false,
        bool includeToolbox = false,
        bool insideCountLabels = false)
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
            color = ExecutivePalette,
            tooltip = new { trigger = "item", formatter = "{b}: {c} artículos ({d}%)" },
            toolbox = includeToolbox ? BuildToolbox() : null,
            legend = new
            {
                orient = compact ? "horizontal" : "vertical",
                left = compact ? "center" : "left",
                top = compact ? "bottom" : "middle",
                itemWidth = 10,
                itemHeight = 10,
                textStyle = new { color = Charcoal, fontSize = compact ? 10 : 12, fontWeight = 700 }
            },
            series = new object[]
            {
                new
                {
                    name = title,
                    type = "pie",
                    radius = compact ? new[] { "46%", "68%" } : new[] { "44%", "70%" },
                    center = compact ? new[] { "50%", "43%" } : new[] { "62%", "52%" },
                    avoidLabelOverlap = true,
                    data,
                    itemStyle = new { borderColor = "#ffffff", borderWidth = compact ? 2 : 3 },
                    emphasis = new { scale = true, scaleSize = compact ? 4 : 6 },
                    label = compact || insideCountLabels
                        ? new { show = true, position = "inside", formatter = "{c}", color = "#ffffff", fontWeight = 900, fontSize = 11 } as object
                        : new { show = true, formatter = "{d}%", color = Ink, fontWeight = 700, fontSize = 11 },
                    labelLine = new { show = !(compact || insideCountLabels), length = 10, length2 = 8 }
                }
            }
        };
    }

    public static object? BuildRoseDonut(IEnumerable<ReportingSummaryItemDto>? items, string title = "Distribución", int take = 8)
    {
        var data = items?
            .Where(x => x.TotalArticles > 0)
            .OrderByDescending(x => x.TotalArticles)
            .Take(take)
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
            color = DonutPalette,
            tooltip = new { trigger = "item", formatter = "{b}: {c} artículos ({d}%)" },
            toolbox = BuildToolbox(),
            legend = new
            {
                bottom = "0",
                left = "center",
                itemWidth = 10,
                itemHeight = 10,
                textStyle = new { color = Charcoal, fontSize = 10, fontWeight = 700 }
            },
            series = new object[]
            {
                new
                {
                    name = title,
                    type = "pie",
                    radius = new[] { "18%", "68%" },
                    center = new[] { "50%", "44%" },
                    roseType = "radius",
                    data,
                    itemStyle = new { borderColor = "#ffffff", borderWidth = 2 },
                    label = new { show = true, formatter = "{c}", color = Ink, fontWeight = 900, fontSize = 11 },
                    labelLine = new { length = 8, length2 = 6 }
                }
            }
        };
    }

    public static object? BuildOpenAccessStackedBar(IReadOnlyList<ReportingOpenAccessByYearDto>? items)
    {
        var seriesItems = items?
            .OrderBy(x => x.Year)
            .ToList();

        if (seriesItems is null || seriesItems.Count == 0)
        {
            return null;
        }

        return new
        {
            color = new[] { Mint, Wine },
            tooltip = new { trigger = "axis", axisPointer = new { type = "shadow" } },
            toolbox = BuildToolbox(),
            legend = new
            {
                top = "2%",
                itemWidth = 10,
                itemHeight = 10,
                textStyle = new { color = Charcoal, fontSize = 11, fontWeight = 700 }
            },
            grid = new { left = "8%", right = "5%", bottom = "11%", top = "20%" },
            xAxis = new
            {
                type = "category",
                data = seriesItems.Select(x => x.Year.ToString()).ToArray(),
                axisTick = new { show = false },
                axisLabel = new { color = Charcoal, fontWeight = 700 }
            },
            yAxis = new
            {
                type = "value",
                axisLabel = new { color = Charcoal },
                splitLine = new { lineStyle = new { color = "rgba(0,54,56,0.10)", type = "dashed" } }
            },
            series = new object[]
            {
                new
                {
                    name = "Open Access",
                    type = "bar",
                    stack = "acceso",
                    data = seriesItems.Select(x => x.OpenAccessArticles).ToArray(),
                    barMaxWidth = 30,
                    itemStyle = new { borderRadius = new[] { 7, 7, 0, 0 } },
                    label = new { show = true, position = "inside", formatter = "{c}", color = "#ffffff", fontWeight = 900 }
                },
                new
                {
                    name = "Restringido",
                    type = "bar",
                    stack = "acceso",
                    data = seriesItems.Select(x => x.NonOpenAccessArticles).ToArray(),
                    barMaxWidth = 30,
                    itemStyle = new { borderRadius = new[] { 7, 7, 0, 0 } },
                    label = new { show = true, position = "inside", formatter = "{c}", color = "#ffffff", fontWeight = 900 }
                }
            }
        };
    }

    public static object? BuildMonthHeatmap(IEnumerable<ReportingSummaryItemDto>? items)
    {
        var parsed = items?
            .Select(ParseMonthItem)
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        if (parsed is null || parsed.Count == 0)
        {
            return null;
        }

        var years = parsed.Select(x => x.Year.ToString()).Distinct().ToArray();
        var data = parsed
            .Select(x => new object[] { x.Month - 1, Array.IndexOf(years, x.Year.ToString()), x.Total })
            .ToArray();

        return new
        {
            tooltip = new
            {
                position = "top",
                formatter = "Mes {b}: {c} artículos"
            },
            toolbox = BuildToolbox(),
            grid = new { left = "9%", right = "5%", bottom = "10%", top = "14%" },
            xAxis = new
            {
                type = "category",
                data = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" },
                splitArea = new { show = true },
                axisLabel = new { color = Charcoal, fontWeight = 700 }
            },
            yAxis = new
            {
                type = "category",
                data = years,
                splitArea = new { show = true },
                axisLabel = new { color = Charcoal, fontWeight = 700 }
            },
            visualMap = new
            {
                min = 0,
                max = Math.Max(1, parsed.Max(x => x.Total)),
                calculable = true,
                orient = "horizontal",
                left = "center",
                bottom = "0",
                inRange = new { color = new[] { "#f1f5f3", "#408A71", "#055052", "#660B05" } }
            },
            series = new object[]
            {
                new
                {
                    name = "Artículos",
                    type = "heatmap",
                    data,
                    label = new { show = true, color = Ink, fontWeight = 900 },
                    emphasis = new { itemStyle = new { shadowBlur = 10, shadowColor = "rgba(0,0,0,0.18)" } }
                }
            }
        };
    }

    public static object? BuildTreemap(IEnumerable<ReportingSummaryItemDto>? items, string title = "Paquetes", int take = 12)
    {
        var data = items?
            .Where(x => x.TotalArticles > 0)
            .OrderByDescending(x => x.TotalArticles)
            .Take(take)
            .Select((x, index) => new Dictionary<string, object>
            {
                ["name"] = x.Name,
                ["value"] = x.TotalArticles,
                ["itemStyle"] = new { color = BarPalette[index % BarPalette.Length] }
            })
            .ToList();

        if (data is null || data.Count == 0)
        {
            return null;
        }

        return new
        {
            tooltip = new { formatter = "{b}: {c} artículos" },
            toolbox = BuildToolbox(),
            series = new object[]
            {
                new
                {
                    name = title,
                    type = "treemap",
                    roam = false,
                    nodeClick = false,
                    breadcrumb = new { show = false },
                    top = "6%",
                    left = "2%",
                    right = "2%",
                    bottom = "4%",
                    data,
                    label = new
                    {
                        show = true,
                        formatter = "{b}\n{c}",
                        color = "#ffffff",
                        fontWeight = 900,
                        fontSize = 11
                    },
                    upperLabel = new { show = false },
                    itemStyle = new { borderColor = "#ffffff", borderWidth = 3, gapWidth = 3 }
                }
            }
        };
    }

    public static object? BuildGauge(string name, int percent, string color = Mint)
    {
        if (percent < 0)
        {
            return null;
        }

        return new
        {
            color = new[] { color },
            tooltip = new { formatter = "{a}: {c}%" },
            series = new object[]
            {
                new
                {
                    name,
                    type = "gauge",
                    startAngle = 210,
                    endAngle = -30,
                    radius = "92%",
                    progress = new { show = true, width = 14, itemStyle = new { color } },
                    axisLine = new { lineStyle = new { width = 14, color = new object[] { new object[] { 1, "rgba(0,54,56,0.12)" } } } },
                    axisTick = new { show = false },
                    splitLine = new { length = 8, lineStyle = new { width = 2, color = "rgba(0,54,56,0.28)" } },
                    axisLabel = new { color = Charcoal, distance = 18, fontSize = 10 },
                    pointer = new { width = 4, itemStyle = new { color = Wine } },
                    anchor = new { show = true, showAbove = true, size = 8, itemStyle = new { color = Wine } },
                    title = new { offsetCenter = new[] { "0", "54%" }, color = Charcoal, fontSize = 12, fontWeight = 800 },
                    detail = new { valueAnimation = true, formatter = "{value}%", color = Ink, fontSize = 22, fontWeight = 950, offsetCenter = new[] { "0", "28%" } },
                    data = new object[] { new { value = percent, name } }
                }
            }
        };
    }

    public static object? BuildRadar(string[]? indicators, int[]? values, string title = "Cobertura")
    {
        if (indicators is null || values is null || indicators.Length == 0 || indicators.Length != values.Length)
        {
            return null;
        }

        var max = Math.Max(1, values.Max());

        return new
        {
            color = new[] { Teal },
            tooltip = new { trigger = "item" },
            radar = new
            {
                radius = "66%",
                center = new[] { "50%", "52%" },
                splitNumber = 4,
                axisName = new { color = Charcoal, fontSize = 11, fontWeight = 800 },
                splitLine = new { lineStyle = new { color = new[] { "rgba(0,54,56,0.08)", "rgba(0,54,56,0.14)" } } },
                splitArea = new { areaStyle = new { color = new[] { "rgba(64,138,113,0.05)", "rgba(255,255,255,0.62)" } } },
                indicator = indicators.Select(x => new { name = x, max = Math.Ceiling(max * 1.2) }).ToArray()
            },
            series = new object[]
            {
                new
                {
                    name = title,
                    type = "radar",
                    symbol = "circle",
                    symbolSize = 6,
                    areaStyle = new { color = "rgba(64,138,113,0.24)" },
                    lineStyle = new { color = Teal, width = 3 },
                    data = new object[] { new { value = values, name = title } }
                }
            }
        };
    }

    public static object? BuildVenueMetricScatter(IEnumerable<ReportingVenueMetricDto>? metrics)
    {
        var data = metrics?
            .Where(x => x.Sjr.HasValue || x.CiteScore.HasValue || x.HIndex.HasValue)
            .OrderByDescending(x => x.Year)
            .Take(40)
            .Select((x, index) => new Dictionary<string, object>
            {
                ["name"] = string.IsNullOrWhiteSpace(x.VenueName) ? "Revista sin nombre" : x.VenueName,
                ["value"] = new object[]
                {
                    Convert.ToDouble(x.Sjr ?? 0),
                    Convert.ToDouble(x.CiteScore ?? 0),
                    x.HIndex ?? 0,
                    x.Year,
                    x.Quartile ?? "Sin cuartil"
                },
                ["symbolSize"] = Math.Clamp((x.HIndex ?? 1) + 8, 10, 42),
                ["itemStyle"] = new { color = BarPalette[index % BarPalette.Length] }
            })
            .ToList();

        if (data is null || data.Count == 0)
        {
            return null;
        }

        return new
        {
            tooltip = new
            {
                trigger = "item",
                formatter = "{b}<br/>SJR: {@[0]}<br/>CiteScore: {@[1]}<br/>H-index: {@[2]}<br/>Año: {@[3]}<br/>Cuartil: {@[4]}"
            },
            toolbox = BuildToolbox(),
            grid = new { left = "10%", right = "6%", bottom = "12%", top = "10%" },
            xAxis = new
            {
                name = "SJR",
                type = "value",
                axisLabel = new { color = Charcoal },
                splitLine = new { lineStyle = new { color = "rgba(0,54,56,0.10)", type = "dashed" } }
            },
            yAxis = new
            {
                name = "CiteScore",
                type = "value",
                axisLabel = new { color = Charcoal },
                splitLine = new { lineStyle = new { color = "rgba(0,54,56,0.10)", type = "dashed" } }
            },
            series = new object[]
            {
                new
                {
                    name = "Revistas",
                    type = "scatter",
                    data,
                    emphasis = new { focus = "series" }
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
            legend = new
            {
                bottom = "0",
                itemWidth = 10,
                itemHeight = 10,
                textStyle = new { color = Charcoal, fontSize = 12, fontWeight = 700 }
            },
            series = new object[]
            {
                new
                {
                    name = "Participación",
                    type = "pie",
                    radius = new[] { "45%", "72%" },
                    center = new[] { "50%", "45%" },
                    data,
                    itemStyle = new { borderColor = "#ffffff", borderWidth = 3 },
                    emphasis = new { scale = true, scaleSize = 6 },
                    label = new { show = true, formatter = "{b}\n{d}%", color = Ink, fontWeight = 700, fontSize = 11 }
                }
            }
        };
    }

    private static (int Year, int Month, int Total)? ParseMonthItem(ReportingSummaryItemDto item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            return null;
        }

        var parts = item.Name.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2 ||
            !int.TryParse(parts[0], out var year) ||
            !int.TryParse(parts[1], out var month) ||
            month is < 1 or > 12)
        {
            return null;
        }

        return (year, month, item.TotalArticles);
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
