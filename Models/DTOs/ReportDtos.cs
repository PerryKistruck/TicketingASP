namespace TicketingASP.Models.DTOs;

#region Dashboard Reports

/// <summary>
/// DTO for dashboard summary statistics
/// </summary>
public class DashboardSummaryDto
{
    public long TotalTickets { get; set; }
    public long OpenTickets { get; set; }
    public long PendingTickets { get; set; }
    public long ResolvedToday { get; set; }
    public long OverdueTickets { get; set; }
    public long UnassignedTickets { get; set; }
    public long CriticalTickets { get; set; }
    public long HighPriorityTickets { get; set; }
    public decimal? AvgResolutionHours { get; set; }
    public long SlaBreachedCount { get; set; }
}

/// <summary>
/// DTO for full dashboard with all data combined
/// </summary>
public class FullDashboardDto
{
    public DashboardSummaryDto? Summary { get; set; }
    public List<TicketsByStatusDto> TicketsByStatus { get; set; } = new();
    public List<TicketsByPriorityDto> TicketsByPriority { get; set; } = new();
    public List<TeamPerformanceDto> TeamPerformance { get; set; } = new();
}

/// <summary>
/// DTO for tickets by status report
/// </summary>
public class TicketsByStatusDto
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public long TicketCount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// DTO for tickets by priority report
/// </summary>
public class TicketsByPriorityDto
{
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public long TicketCount { get; set; }
    public long OpenCount { get; set; }
    public decimal AvgResolutionHours { get; set; }
}

/// <summary>
/// DTO for tickets by category report
/// </summary>
public class TicketsByCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? ParentCategory { get; set; }
    public long TicketCount { get; set; }
    public long OpenCount { get; set; }
}

#endregion

#region Performance Reports

/// <summary>
/// DTO for team performance report
/// </summary>
public class TeamPerformanceDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public long AssignedTickets { get; set; }
    public long ResolvedTickets { get; set; }
    public long OpenTickets { get; set; }
    public long OverdueTickets { get; set; }
    public decimal AvgResolutionHours { get; set; }
    public decimal AvgResponseHours { get; set; }
    public decimal? SlaCompliancePercent { get; set; }
}

/// <summary>
/// DTO for agent performance report
/// </summary>
public class AgentPerformanceDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public long TotalAssigned { get; set; }
    public long ResolvedCount { get; set; }
    public long OpenCount { get; set; }
    public decimal? AvgResolutionHours { get; set; }
    public decimal? AvgFirstResponseHours { get; set; }
}

/// <summary>
/// DTO for ticket trend report
/// </summary>
public class TicketTrendDto
{
    public DateTime PeriodDate { get; set; }
    public long CreatedCount { get; set; }
    public long ResolvedCount { get; set; }
    public long ClosedCount { get; set; }
}

/// <summary>
/// DTO for SLA compliance report
/// </summary>
public class SlaComplianceDto
{
    public string PriorityName { get; set; } = string.Empty;
    public long TotalTickets { get; set; }
    public long WithinSlaResponse { get; set; }
    public long BreachedSlaResponse { get; set; }
    public long WithinSlaResolution { get; set; }
    public long BreachedSlaResolution { get; set; }
    public decimal? ResponseCompliancePercent { get; set; }
    public decimal? ResolutionCompliancePercent { get; set; }
}

#endregion

#region Report Filters

/// <summary>
/// DTO for report date range filter
/// </summary>
public class ReportFilterDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? TeamId { get; set; }
    public string? Period { get; set; } = "daily"; // daily, weekly, monthly
}

/// <summary>
/// ViewModel for unified reports page
/// </summary>
public class UnifiedReportsViewModel
{
    public string ActiveTab { get; set; } = "sla";
    public ReportFilterDto Filter { get; set; } = new();
    public List<LookupItemDto> Teams { get; set; } = new();
    public List<SlaComplianceDto> SlaCompliance { get; set; } = new();
    public List<TeamPerformanceDto> TeamPerformance { get; set; } = new();
    public List<AgentPerformanceDto> AgentPerformance { get; set; } = new();
}

#endregion
