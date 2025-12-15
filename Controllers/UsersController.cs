using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingASP.Models.DTOs;
using TicketingASP.Services;

namespace TicketingASP.Controllers;

/// <summary>
/// Controller for user management operations - restricted to Administrators
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly ILookupService _lookupService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILookupService lookupService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _lookupService = lookupService;
        _logger = logger;
    }

    /// <summary>
    /// Display user list
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null)
    {
        var users = await _userService.GetUsersAsync(page, pageSize, search, isActive);

        ViewBag.Search = search;
        ViewBag.IsActive = isActive;

        return View(users);
    }

    /// <summary>
    /// Display user details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    /// <summary>
    /// Display registration form for new support staff
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = await _lookupService.GetRolesAsync();
        return View(new RegisterUserDto());
    }

    /// <summary>
    /// Register a new support staff user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _lookupService.GetRolesAsync();
            return View(dto);
        }

        var createdBy = GetCurrentUserId();
        var result = await _userService.RegisterAsync(dto, createdBy);

        if (!result.Success)
        {
            ViewBag.Roles = await _lookupService.GetRolesAsync();
            ModelState.AddModelError(string.Empty, result.Message);
            return View(dto);
        }

        TempData["SuccessMessage"] = "User created successfully!";
        return RedirectToAction(nameof(Details), new { id = result.Data });
    }

    /// <summary>
    /// Display edit user form
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        
        if (user == null)
        {
            return NotFound();
        }

        var dto = new UpdateUserDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl
        };

        ViewBag.UserId = id;
        ViewBag.Email = user.Email;

        return View(dto);
    }

    /// <summary>
    /// Update a user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.UserId = id;
            return View(dto);
        }

        var updatedBy = GetCurrentUserId();
        var result = await _userService.UpdateUserAsync(id, dto, updatedBy);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.UserId = id;
            return View(dto);
        }

        TempData["SuccessMessage"] = "User updated successfully!";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Delete a user (soft delete)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var deletedBy = GetCurrentUserId();
        var result = await _userService.DeleteUserAsync(id, deletedBy);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "User deactivated successfully!";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Assign role to user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(int id, int roleId)
    {
        var assignedBy = GetCurrentUserId();
        var result = await _userService.AssignRoleAsync(id, roleId, assignedBy);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Role assigned successfully!";
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
