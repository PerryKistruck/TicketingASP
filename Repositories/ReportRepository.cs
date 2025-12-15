using Dapper;
using TicketingASP.Data;
using TicketingASP.Models.DTOs;

namespace TicketingASP.Repositories;

/// <summary>
/// Repository interface for report operations using stored procedures
/// </summary>
public interface IReportRepository
{
    Task<DashboardSummaryDto?> GetDashboardSummaryAsync(int? userId, string userRole, int? teamId = null);
    Task<List<TicketsByStatusDto>> GetTicketsByStatusAsync(DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<List<TicketsByPriorityDto>> GetTicketsByPriorityAsync(DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<List<TicketsByCategoryDto>> GetTicketsByCategoryAsync(DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<List<TeamPerformanceDto>> GetTeamPerformanceAsync(DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<List<AgentPerformanceDto>> GetAgentPerformanceAsync(int? teamId = null, DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<List<TicketTrendDto>> GetTicketTrendAsync(string period = "daily", DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<List<SlaComplianceDto>> GetSlaComplianceAsync(DateTime? dateFrom = null, DateTime? dateTo = null);
}

/// <summary>
/// Implementation of report repository using PostgreSQL stored procedures
/// </summary>
public class ReportRepository : IReportRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ReportRepository> _logger;

    public ReportRepository(IDbConnectionFactory connectionFactory, ILogger<ReportRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto?> GetDashboardSummaryAsync(int? userId, string userRole, int? teamId = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            return await connection.QueryFirstOrDefaultAsync<DashboardSummaryDto>(
                @"SELECT total_tickets AS TotalTickets, open_tickets AS OpenTickets, pending_tickets AS PendingTickets,
                         resolved_today AS ResolvedToday, overdue_tickets AS OverdueTickets, unassigned_tickets AS UnassignedTickets,
                         avg_resolution_hours AS AvgResolutionHours, sla_breached_count AS SlaBreachedCount
                  FROM sp_report_dashboard_summary(@p_user_id, @p_user_role, @p_team_id)",
                new { p_user_id = userId, p_user_role = userRole, p_team_id = teamId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard summary");
            return null;
        }
    }

    public async Task<List<TicketsByStatusDto>> GetTicketsByStatusAsync(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<TicketsByStatusDto>(
                @"SELECT status_id AS StatusId, status_name AS StatusName, status_color AS Colour,
                         ticket_count AS TicketCount, percentage AS Percentage
                  FROM sp_report_tickets_by_status(@p_date_from, @p_date_to)",
                new { p_date_from = dateFrom, p_date_to = dateTo });

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tickets by status report");
            return new List<TicketsByStatusDto>();
        }
    }

    public async Task<List<TicketsByPriorityDto>> GetTicketsByPriorityAsync(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<TicketsByPriorityDto>(
                @"SELECT priority_id AS PriorityId, priority_name AS PriorityName, priority_color AS Colour,
                         ticket_count AS TicketCount, open_count AS OpenCount, avg_resolution_hours AS AvgResolutionHours
                  FROM sp_report_tickets_by_priority(@p_date_from, @p_date_to)",
                new { p_date_from = dateFrom, p_date_to = dateTo });

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tickets by priority report");
            return new List<TicketsByPriorityDto>();
        }
    }

    public async Task<List<TicketsByCategoryDto>> GetTicketsByCategoryAsync(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<TicketsByCategoryDto>(
                @"SELECT category_id AS CategoryId, category_name AS CategoryName, parent_category AS ParentCategory,
                         ticket_count AS TicketCount, open_count AS OpenCount
                  FROM sp_report_tickets_by_category(@p_date_from, @p_date_to)",
                new { p_date_from = dateFrom, p_date_to = dateTo });

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tickets by category report");
            return new List<TicketsByCategoryDto>();
        }
    }

    public async Task<List<TeamPerformanceDto>> GetTeamPerformanceAsync(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<TeamPerformanceDto>(
                @"SELECT team_id AS TeamId, team_name AS TeamName, total_assigned AS AssignedTickets,
                         resolved_count AS ResolvedTickets, open_count AS OpenTickets, overdue_count AS OverdueTickets,
                         avg_resolution_hours AS AvgResolutionHours, avg_first_response_hours AS AvgResponseHours,
                         sla_compliance_percent AS SlaCompliancePercent
                  FROM sp_report_team_performance(@p_date_from, @p_date_to)",
                new { p_date_from = dateFrom, p_date_to = dateTo });

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team performance report");
            return new List<TeamPerformanceDto>();
        }
    }

    public async Task<List<AgentPerformanceDto>> GetAgentPerformanceAsync(int? teamId = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<AgentPerformanceDto>(
                @"SELECT user_id AS UserId, user_name AS UserName, team_name AS TeamName,
                         total_assigned AS TotalAssigned, resolved_count AS ResolvedCount, open_count AS OpenCount,
                         avg_resolution_hours AS AvgResolutionHours, avg_first_response_hours AS AvgFirstResponseHours
                  FROM sp_report_agent_performance(@p_team_id, @p_date_from, @p_date_to)",
                new { p_team_id = teamId, p_date_from = dateFrom, p_date_to = dateTo });

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent performance report");
            return new List<AgentPerformanceDto>();
        }
    }

    public async Task<List<TicketTrendDto>> GetTicketTrendAsync(string period = "daily", DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<TicketTrendDto>(
                @"SELECT period_date AS PeriodDate, created_count AS CreatedCount,
                         resolved_count AS ResolvedCount, closed_count AS ClosedCount
                  FROM sp_report_ticket_trend(@p_period, @p_date_from, @p_date_to)",
                new { p_period = period, p_date_from = dateFrom ?? DateTime.UtcNow.AddDays(-30), p_date_to = dateTo ?? DateTime.UtcNow });

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket trend report");
            return new List<TicketTrendDto>();
        }
    }

    public async Task<List<SlaComplianceDto>> GetSlaComplianceAsync(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<SlaComplianceDto>(
                @"SELECT priority_name AS PriorityName, total_tickets AS TotalTickets,
                         within_sla_response AS WithinSlaResponse, breached_sla_response AS BreachedSlaResponse,
                         within_sla_resolution AS WithinSlaResolution, breached_sla_resolution AS BreachedSlaResolution,
                         response_compliance_percent AS ResponseCompliancePercent, resolution_compliance_percent AS ResolutionCompliancePercent
                  FROM sp_report_sla_compliance(@p_date_from, @p_date_to)",
                new { p_date_from = dateFrom, p_date_to = dateTo });

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SLA compliance report");
            return new List<SlaComplianceDto>();
        }
    }
}
