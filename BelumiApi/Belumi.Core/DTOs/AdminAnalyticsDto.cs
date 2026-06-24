using System;
using System.Collections.Generic;

namespace Belumi.Core.DTOs;

public sealed class AdminAnalyticsDto
{
    public TotalOverviewDto Overview { get; set; } = new();
    public List<TimeSeriesPointDto> TimeSeries { get; set; } = [];
    public DistributionDto Distributions { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = [];
}

public sealed class TotalOverviewDto
{
    public decimal TotalRevenue { get; set; }
    public double RevenueGrowthPercent { get; set; }
    public int NewUsers { get; set; }
    public double UserGrowthPercent { get; set; }
    public int TotalScans { get; set; }
    public double ScanGrowthPercent { get; set; }
    public double ConversionRate { get; set; }
    public int PremiumUsersCount { get; set; }
    public int TotalUsers { get; set; }
    public int PremiumPurchases { get; set; }
    public int TotalArticles { get; set; }
    public int NewArticles { get; set; }
}

public sealed class TimeSeriesPointDto
{
    public string Label { get; set; } = string.Empty; // e.g. "2026-06-24", "Jun 2026", "2026"
    public decimal Revenue { get; set; }
    public int NewUsers { get; set; }
    public int Scans { get; set; }
    public int PremiumPurchases { get; set; }
    public int NewArticles { get; set; }
}

public sealed class DistributionDto
{
    public List<PieItemDto> SubscriptionPlans { get; set; } = [];
    public List<PieItemDto> SkinTypes { get; set; } = [];
    public List<BarItemDto> TopIngredients { get; set; } = [];
}

public sealed class PieItemDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public sealed class BarItemDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class RecentActivityDto
{
    public string Type { get; set; } = string.Empty; // "payment", "signup", "scan"
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
