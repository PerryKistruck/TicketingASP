using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingASP.Models.DTOs;
using TicketingASP.Services;

namespace TicketingASP.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IUserService userService, ILogger<AccountController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Display login form
    /// </summary>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginDto());
    }

    /// <summary>
    /// Process login
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _userService.LoginAsync(dto, ipAddress, userAgent);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(dto);
        }

        // Create claims for the authenticated user
        var user = result.Data!;
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email)
        };

        // Add role claims
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }

        var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = dto.RememberMe,
            ExpiresUtc = dto.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync("Cookies", claimsPrincipal, authProperties);
        
        _logger.LogInformation("User {Email} logged in successfully", dto.Email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Display registration form
    /// </summary>
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterUserDto());
    }

    /// <summary>
    /// Process registration
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _userService.RegisterAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(dto);
        }

        TempData["SuccessMessage"] = "Registration successful! Please login.";
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Logout user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        _logger.LogInformation("User {Name} logged out", User.Identity?.Name);

        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Display user profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(user);
    }

    /// <summary>
    /// Display edit profile form
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var dto = new UpdateUserDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl
        };

        return View(dto);
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _userService.UpdateUserAsync(userId, dto, userId);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(dto);
        }

        TempData["SuccessMessage"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Profile));
    }

    /// <summary>
    /// Display change password form
    /// </summary>
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordDto());
    }

    /// <summary>
    /// Process password change
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _userService.ChangePasswordAsync(userId, dto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(dto);
        }

        TempData["SuccessMessage"] = "Password changed successfully!";
        return RedirectToAction(nameof(Profile));
    }

    /// <summary>
    /// Access denied page
    /// </summary>
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    #region Helper Methods

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    #endregion
}
