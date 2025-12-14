using Microsoft.AspNetCore.Mvc;
using TicketingASP.Models.DTOs;
using TicketingASP.Services;

namespace TicketingASP.Controllers;

/// <summary>
/// Controller for reports and dashboard
/// </summary>
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly ILookupService _lookupService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        ILookupService lookupService,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _lookupService = lookupService;
        _logger = logger;
    }

    /// <summary>
    /// Display main dashboard
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var dashboard = await _reportService.GetFullDashboardAsync(userId, userRole);

        return View(dashboard);
    }

    /// <summary>
    /// Display tickets by status report
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TicketsByStatus([FromQuery] ReportFilterDto filter)
    {
        var report = await _reportService.GetTicketsByStatusAsync(filter);
        ViewBag.Filter = filter;
        return View(report);
    }

    /// <summary>
    /// Display tickets by priority report
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TicketsByPriority([FromQuery] ReportFilterDto filter)
    {
        var report = await _reportService.GetTicketsByPriorityAsync(filter);
        ViewBag.Filter = filter;
        return View(report);
    }

    /// <summary>
    /// Display tickets by category report
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TicketsByCategory([FromQuery] ReportFilterDto filter)
    {
        var report = await _reportService.GetTicketsByCategoryAsync(filter);
        ViewBag.Filter = filter;
        return View(report);
    }

    /// <summary>
    /// Display team performance report
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TeamPerformance([FromQuery] ReportFilterDto filter)
    {
        var report = await _reportService.GetTeamPerformanceAsync(filter);
        ViewBag.Filter = filter;
        return View(report);
    }

    /// <summary>
    /// Display agent performance report
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> AgentPerformance([FromQuery] ReportFilterDto filter)
    {
        var teams = await _lookupService.GetTeamsAsync();
        ViewBag.Teams = teams;
        
        var report = await _reportService.GetAgentPerformanceAsync(filter);
        ViewBag.Filter = filter;
        return View(report);
    }

    /// <summary>
    /// Display ticket trend report
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TicketTrend([FromQuery] ReportFilterDto filter)
    {
        filter ??= new ReportFilterDto();
        filter.Period ??= "daily";
        filter.DateFrom ??= DateTime.UtcNow.AddDays(-30);
        filter.DateTo ??= DateTime.UtcNow;

        var report = await _reportService.GetTicketTrendAsync(filter);
        ViewBag.Filter = filter;
        return View(report);
    }

    /// <summary>
    /// Display SLA compliance report
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SlaCompliance([FromQuery] ReportFilterDto filter)
    {
        var report = await _reportService.GetSlaComplianceAsync(filter);
        ViewBag.Filter = filter;
        return View(report);
    }

    #region API Endpoints for Charts

    /// <summary>
    /// Get dashboard summary data as JSON
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var summary = await _reportService.GetDashboardSummaryAsync(userId, userRole);
        return Json(summary);
    }

    /// <summary>
    /// Get tickets by status data as JSON
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTicketsByStatusData([FromQuery] ReportFilterDto filter)
    {
        var data = await _reportService.GetTicketsByStatusAsync(filter);
        return Json(data);
    }

    /// <summary>
    /// Get tickets by priority data as JSON
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTicketsByPriorityData([FromQuery] ReportFilterDto filter)
    {
        var data = await _reportService.GetTicketsByPriorityAsync(filter);
        return Json(data);
    }

    /// <summary>
    /// Get ticket trend data as JSON
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTicketTrendData([FromQuery] ReportFilterDto filter)
    {
        var data = await _reportService.GetTicketTrendAsync(filter);
        return Json(data);
    }

    #endregion

    #region Helper Methods

    private int GetCurrentUserId()
    {
        // TODO: Implement proper authentication
        return 1;
    }

    private string GetCurrentUserRole()
    {
        // TODO: Implement proper authentication
        return "Administrator";
    }

    #endregion
}
