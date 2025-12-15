using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingASP.Models.DTOs;
using TicketingASP.Services;

namespace TicketingASP.Controllers;

/// <summary>
/// Controller for team management operations - restricted to Administrators
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class TeamsController : Controller
{
    private readonly ITeamService _teamService;
    private readonly IUserService _userService;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(
        ITeamService teamService, 
        IUserService userService,
        ILogger<TeamsController> logger)
    {
        _teamService = teamService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Display team list
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? search = null, bool? isActive = true)
    {
        var teams = await _teamService.GetTeamsAsync(page, pageSize, search, isActive);

        ViewBag.Search = search;
        ViewBag.IsActive = isActive;

        return View(teams);
    }

    /// <summary>
    /// Display team details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var team = await _teamService.GetTeamByIdAsync(id);
        
        if (team == null)
        {
            return NotFound();
        }

        var members = await _teamService.GetMembersAsync(id);
        ViewBag.Members = members;

        // Get users for Add Member dropdown
        var users = await _userService.GetUsersAsync(1, 100, null, true);
        ViewBag.Users = users.Items;

        return View(team);
    }

    /// <summary>
    /// Display create team form
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        // Get users for manager dropdown
        var users = await _userService.GetUsersAsync(1, 100, null, true);
        ViewBag.Users = users.Items;

        return View(new CreateTeamDto());
    }

    /// <summary>
    /// Create a new team
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTeamDto dto)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userService.GetUsersAsync(1, 100, null, true);
            ViewBag.Users = users.Items;
            return View(dto);
        }

        var createdBy = GetCurrentUserId();
        var result = await _teamService.CreateTeamAsync(dto, createdBy);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            var users = await _userService.GetUsersAsync(1, 100, null, true);
            ViewBag.Users = users.Items;
            return View(dto);
        }

        TempData["SuccessMessage"] = "Team created successfully!";
        return RedirectToAction(nameof(Details), new { id = result.Data });
    }

    /// <summary>
    /// Display edit team form
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var team = await _teamService.GetTeamByIdAsync(id);
        
        if (team == null)
        {
            return NotFound();
        }

        var users = await _userService.GetUsersAsync(1, 100, null, true);
        ViewBag.Users = users.Items;
        ViewBag.TeamId = id;

        var dto = new UpdateTeamDto
        {
            Name = team.Name,
            Description = team.Description,
            Email = team.Email,
            IsActive = team.IsActive
        };

        return View(dto);
    }

    /// <summary>
    /// Update a team
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateTeamDto dto)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userService.GetUsersAsync(1, 100, null, true);
            ViewBag.Users = users.Items;
            ViewBag.TeamId = id;
            return View(dto);
        }

        var updatedBy = GetCurrentUserId();
        var result = await _teamService.UpdateTeamAsync(id, dto, updatedBy);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            var users = await _userService.GetUsersAsync(1, 100, null, true);
            ViewBag.Users = users.Items;
            ViewBag.TeamId = id;
            return View(dto);
        }

        TempData["SuccessMessage"] = "Team updated successfully!";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Delete a team (soft delete)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var deletedBy = GetCurrentUserId();
        var result = await _teamService.DeleteTeamAsync(id, deletedBy);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Team deactivated successfully!";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Add member to team
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(int id, AddTeamMemberDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please select a valid user.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var addedBy = GetCurrentUserId();
        var result = await _teamService.AddMemberAsync(id, dto, addedBy);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Team member added successfully!";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Remove member from team
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        var result = await _teamService.RemoveMemberAsync(id, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Team member removed successfully!";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    #region Helper Methods

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Unable to retrieve current user ID from claims");
            throw new UnauthorizedAccessException("User is not authenticated or user ID claim is missing.");
        }
        
        return userId;
    }

    #endregion
}
