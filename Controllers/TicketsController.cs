using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingASP.Models.DTOs;
using TicketingASP.Services;
using System.Security.Claims;

namespace TicketingASP.Controllers;

/// <summary>
/// Controller for ticket management operations
/// </summary>
public class TicketsController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly ILookupService _lookupService;
    private readonly ITeamService _teamService;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        ITicketService ticketService, 
        ILookupService lookupService,
        ITeamService teamService,
        ILogger<TicketsController> logger)
    {
        _ticketService = ticketService;
        _lookupService = lookupService;
        _teamService = teamService;
        _logger = logger;
    }

    /// <summary>
    /// Display "My Tickets" - tickets relevant to the current user
    /// Supports tabs: "mine" for personal tickets, "team" for team tickets
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] TicketFilterDto filter, string tab = "mine", int? teamId = null)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        // Get user's teams for the team tab
        var userTeams = await _teamService.GetUserTeamsAsync(userId);
        var hasTeams = userTeams.Any();

        // If a specific status is selected, include closed tickets in the query
        // so the user can see resolved/closed tickets when they explicitly filter for them
        if (filter.StatusId.HasValue)
        {
            filter.IncludeClosed = true;
        }

        if (tab == "team" && hasTeams)
        {
            // Show tickets assigned to user's teams
            filter.AssignedToId = null;
            filter.RequesterId = null;
            
            // If user specified a team, use that; otherwise use first team
            if (teamId.HasValue && userTeams.Any(t => t.Id == teamId.Value))
            {
                filter.AssignedTeamId = teamId.Value;
            }
            else
            {
                filter.AssignedTeamId = userTeams.First().Id;
                teamId = userTeams.First().Id;
            }
        }
        else
        {
            // Default "mine" tab
            if (userRole == "User")
            {
                // Regular users see only tickets they created
                filter.RequesterId = userId;
            }
            else
            {
                // Agents, Managers, Admins see tickets assigned to them
                filter.AssignedToId = userId;
            }
            tab = "mine"; // Reset to mine if team tab was requested but user has no teams
        }

        var tickets = await _ticketService.GetTicketsAsync(filter, userId, userRole);
        var lookups = await _lookupService.GetAllLookupsAsync();

        ViewBag.Filter = filter;
        ViewBag.Lookups = lookups;
        ViewBag.UserRole = userRole;
        ViewBag.ViewType = "MyTickets";
        ViewBag.ActiveTab = tab;
        ViewBag.UserTeams = userTeams;
        ViewBag.HasTeams = hasTeams;
        ViewBag.SelectedTeamId = teamId;

        return View(tickets);
    }

    /// <summary>
    /// Display all tickets - available to Managers and Admins for global ticket management
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> All([FromQuery] TicketFilterDto filter)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        // If a specific status is selected, include closed tickets in the query
        if (filter.StatusId.HasValue)
        {
            filter.IncludeClosed = true;
        }

        // No filtering - show all tickets
        var tickets = await _ticketService.GetTicketsAsync(filter, userId, userRole);
        var lookups = await _lookupService.GetAllLookupsAsync();

        ViewBag.Filter = filter;
        ViewBag.Lookups = lookups;
        ViewBag.UserRole = userRole;
        ViewBag.ViewType = "AllTickets";

        return View("Index", tickets);
    }

    /// <summary>
    /// Display ticket details
    /// Users can only view their own tickets
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        
        if (ticket == null)
        {
            return NotFound();
        }

        // Regular users can only view their own tickets
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        if (userRole == "User" && ticket.CreatedById != userId)
        {
            return Forbid();
        }

        var lookups = await _lookupService.GetAllLookupsAsync();
        ViewBag.Lookups = lookups;
        ViewBag.UserRole = userRole;

        return View(ticket);
    }

    /// <summary>
    /// Display create ticket form - all authenticated users can create tickets
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var lookups = await _lookupService.GetAllLookupsAsync();
        ViewBag.Lookups = lookups;

        return View(new CreateTicketDto());
    }

    /// <summary>
    /// Create a new ticket
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketDto dto)
    {
        if (!ModelState.IsValid)
        {
            var lookups = await _lookupService.GetAllLookupsAsync();
            ViewBag.Lookups = lookups;
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();

        var result = await _ticketService.CreateTicketAsync(dto, userId, userId, ipAddress);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            var lookups = await _lookupService.GetAllLookupsAsync();
            ViewBag.Lookups = lookups;
            return View(dto);
        }

        TempData["SuccessMessage"] = $"Ticket {result.Data?.TicketNumber} created successfully!";
        return RedirectToAction(nameof(Details), new { id = result.Data?.Id });
    }

    /// <summary>
    /// Display edit ticket form - only Agents, Managers, and Admins can edit
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AgentOrAbove")]
    public async Task<IActionResult> Edit(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        
        if (ticket == null)
        {
            return NotFound();
        }

        var lookups = await _lookupService.GetAllLookupsAsync();
        ViewBag.Lookups = lookups;

        var dto = new UpdateTicketDto
        {
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            PriorityId = ticket.PriorityId,
            StatusId = ticket.StatusId,
            AssignedToId = ticket.AssignedToId,
            AssignedTeamId = ticket.AssignedTeamId,
            DueDate = ticket.DueDate,
            Tags = ticket.Tags
        };

        ViewBag.TicketId = id;
        ViewBag.TicketNumber = ticket.TicketNumber;

        return View(dto);
    }

    /// <summary>
    /// Update a ticket - only Agents, Managers, and Admins can update
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AgentOrAbove")]
    public async Task<IActionResult> Edit(int id, UpdateTicketDto dto)
    {
        if (!ModelState.IsValid)
        {
            var lookups = await _lookupService.GetAllLookupsAsync();
            ViewBag.Lookups = lookups;
            ViewBag.TicketId = id;
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();

        var result = await _ticketService.UpdateTicketAsync(id, dto, userId, ipAddress);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            var lookups = await _lookupService.GetAllLookupsAsync();
            ViewBag.Lookups = lookups;
            ViewBag.TicketId = id;
            return View(dto);
        }

        TempData["SuccessMessage"] = "Ticket updated successfully!";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Delete a ticket
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();

        var result = await _ticketService.DeleteTicketAsync(id, userId, ipAddress);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Ticket deleted successfully!";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Add a comment to a ticket
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int id, AddCommentDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please enter a valid comment.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var userId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();

        var result = await _ticketService.AddCommentAsync(id, userId, dto, ipAddress);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Comment added successfully!";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Change ticket status (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ChangeStatus(int id, int statusId)
    {
        var userId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();

        var result = await _ticketService.ChangeStatusAsync(id, statusId, userId, ipAddress);

        return Json(new { success = result.Success, message = result.Message });
    }

    /// <summary>
    /// Assign ticket (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Assign(int id, int? assignedToId, int? assignedTeamId)
    {
        var userId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();

        var result = await _ticketService.AssignTicketAsync(id, assignedToId, assignedTeamId, userId, ipAddress);

        return Json(new { success = result.Success, message = result.Message });
    }

    #region Helper Methods

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private string GetCurrentUserRole()
    {
        // Return the highest role the user has
        if (User.IsInRole("Administrator")) return "Administrator";
        if (User.IsInRole("Manager")) return "Manager";
        if (User.IsInRole("Agent")) return "Agent";
        return "User";
    }

    private string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
}
