using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingASP.Models.DTOs;
using TicketingASP.Services;
using System.Security.Claims;

namespace TicketingASP.Controllers;

/// <summary>
/// Controller for reports and dashboard
/// Dashboard is accessible to Agents and above, Reports are restricted to Managers and Administrators
/// </summary>
[Authorize]
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
    /// Display main dashboard - accessible to Agents and above
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AgentOrAbove")]
    public async Task<IActionResult> Dashboard()
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var dashboard = await _reportService.GetFullDashboardAsync(userId, userRole);

        return View(dashboard);
    }

    /// <summary>
    /// Display unified reports page with tabs - accessible to Managers and Administrators only
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> Index([FromQuery] ReportFilterDto? filter, string? tab = "sla")
    {
        filter ??= new ReportFilterDto
        {
            DateFrom = DateTime.UtcNow.AddDays(-30),
            DateTo = DateTime.UtcNow
        };

        var viewModel = new UnifiedReportsViewModel
        {
            ActiveTab = tab ?? "sla",
            Filter = filter,
            Teams = await _lookupService.GetTeamsAsync()
        };

        // Load data based on active tab (or load all for initial render)
        viewModel.SlaCompliance = await _reportService.GetSlaComplianceAsync(filter);
        viewModel.TeamPerformance = await _reportService.GetTeamPerformanceAsync(filter);
        viewModel.AgentPerformance = await _reportService.GetAgentPerformanceAsync(filter);

        return View(viewModel);
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private string GetCurrentUserRole()
    {
        if (User.IsInRole("Administrator")) return "Administrator";
        if (User.IsInRole("Manager")) return "Manager";
        if (User.IsInRole("Agent")) return "Agent";
        return "User";
    }

    #endregion
}
