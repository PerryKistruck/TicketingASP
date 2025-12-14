using TicketingASP.Models.DTOs;
using TicketingASP.Repositories;

namespace TicketingASP.Services;

/// <summary>
/// Service interface for reporting operations
/// </summary>
public interface IReportService
{
    Task<DashboardSummaryDto?> GetDashboardSummaryAsync(int? userId, string userRole, int? teamId = null);
    Task<List<TicketsByStatusDto>> GetTicketsByStatusAsync(ReportFilterDto? filter = null);
    Task<List<TicketsByPriorityDto>> GetTicketsByPriorityAsync(ReportFilterDto? filter = null);
    Task<List<TicketsByCategoryDto>> GetTicketsByCategoryAsync(ReportFilterDto? filter = null);
    Task<List<TeamPerformanceDto>> GetTeamPerformanceAsync(ReportFilterDto? filter = null);
    Task<List<AgentPerformanceDto>> GetAgentPerformanceAsync(ReportFilterDto? filter = null);
    Task<List<TicketTrendDto>> GetTicketTrendAsync(ReportFilterDto? filter = null);
    Task<List<SlaComplianceDto>> GetSlaComplianceAsync(ReportFilterDto? filter = null);
    Task<DashboardReportDto> GetFullDashboardAsync(int? userId, string userRole, int? teamId = null);
}

/// <summary>
/// Combined dashboard report DTO
/// </summary>
public class DashboardReportDto
{
    public DashboardSummaryDto? Summary { get; set; }
    public List<TicketsByStatusDto> TicketsByStatus { get; set; } = new();
    public List<TicketsByPriorityDto> TicketsByPriority { get; set; } = new();
    public List<TicketTrendDto> TicketTrend { get; set; } = new();
}

/// <summary>
/// Implementation of report service
/// </summary>
public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IReportRepository reportRepository, ILogger<ReportService> logger)
    {
        _reportRepository = reportRepository;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto?> GetDashboardSummaryAsync(int? userId, string userRole, int? teamId = null)
    {
        return await _reportRepository.GetDashboardSummaryAsync(userId, userRole, teamId);
    }

    public async Task<List<TicketsByStatusDto>> GetTicketsByStatusAsync(ReportFilterDto? filter = null)
    {
        return await _reportRepository.GetTicketsByStatusAsync(filter?.DateFrom, filter?.DateTo);
    }

    public async Task<List<TicketsByPriorityDto>> GetTicketsByPriorityAsync(ReportFilterDto? filter = null)
    {
        return await _reportRepository.GetTicketsByPriorityAsync(filter?.DateFrom, filter?.DateTo);
    }

    public async Task<List<TicketsByCategoryDto>> GetTicketsByCategoryAsync(ReportFilterDto? filter = null)
    {
        return await _reportRepository.GetTicketsByCategoryAsync(filter?.DateFrom, filter?.DateTo);
    }

    public async Task<List<TeamPerformanceDto>> GetTeamPerformanceAsync(ReportFilterDto? filter = null)
    {
        return await _reportRepository.GetTeamPerformanceAsync(filter?.DateFrom, filter?.DateTo);
    }

    public async Task<List<AgentPerformanceDto>> GetAgentPerformanceAsync(ReportFilterDto? filter = null)
    {
        return await _reportRepository.GetAgentPerformanceAsync(filter?.TeamId, filter?.DateFrom, filter?.DateTo);
    }

    public async Task<List<TicketTrendDto>> GetTicketTrendAsync(ReportFilterDto? filter = null)
    {
        return await _reportRepository.GetTicketTrendAsync(filter?.Period ?? "daily", filter?.DateFrom, filter?.DateTo);
    }

    public async Task<List<SlaComplianceDto>> GetSlaComplianceAsync(ReportFilterDto? filter = null)
    {
        return await _reportRepository.GetSlaComplianceAsync(filter?.DateFrom, filter?.DateTo);
    }

    public async Task<DashboardReportDto> GetFullDashboardAsync(int? userId, string userRole, int? teamId = null)
    {
        _logger.LogInformation("Generating full dashboard for user {UserId} with role {Role}", userId, userRole);

        var summaryTask = GetDashboardSummaryAsync(userId, userRole, teamId);
        var statusTask = GetTicketsByStatusAsync();
        var priorityTask = GetTicketsByPriorityAsync();
        var trendTask = GetTicketTrendAsync(new ReportFilterDto { Period = "daily" });

        await Task.WhenAll(summaryTask, statusTask, priorityTask, trendTask);

        return new DashboardReportDto
        {
            Summary = await summaryTask,
            TicketsByStatus = await statusTask,
            TicketsByPriority = await priorityTask,
            TicketTrend = await trendTask
        };
    }
}
