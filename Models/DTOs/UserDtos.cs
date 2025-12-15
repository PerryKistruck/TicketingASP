using System.ComponentModel.DataAnnotations;

namespace TicketingASP.Models.DTOs;

#region User DTOs

/// <summary>
/// DTO for user registration
/// </summary>
public class RegisterUserDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$", 
        ErrorMessage = "Password must be at least 8 characters and contain at least one letter, one number, and one special character")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    public string? Phone { get; set; }
}

/// <summary>
/// DTO for user login
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// DTO for updating user profile
/// </summary>
public class UpdateUserDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    public string? Phone { get; set; }

    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500, ErrorMessage = "Avatar URL cannot exceed 500 characters")]
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// DTO for changing password
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$", 
        ErrorMessage = "Password must be at least 8 characters and contain at least one letter, one number, and one special character")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO for user list display
/// </summary>
public class UserListDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Roles { get; set; } = string.Empty;
}

/// <summary>
/// DTO for user details
/// </summary>
public class UserDetailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RoleDto> Roles { get; set; } = new();
    public List<TeamDto> Teams { get; set; } = new();
}

/// <summary>
/// DTO for role display
/// </summary>
public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

#endregion

#region Team DTOs

/// <summary>
/// DTO for creating a team
/// </summary>
public class CreateTeamDto
{
    [Required(ErrorMessage = "Team name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Team name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public int? ManagerId { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string? Email { get; set; }
}

/// <summary>
/// DTO for updating a team
/// </summary>
public class UpdateTeamDto
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Team name must be between 2 and 100 characters")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public int? ManagerId { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string? Email { get; set; }

    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for team list display
/// </summary>
public class TeamDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ManagerName { get; set; }
    public string? Email { get; set; }
    public int MemberCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for team member display
/// </summary>
public class TeamMemberDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsTeamLead { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// DTO for adding a team member
/// </summary>
public class AddTeamMemberDto
{
    [Required(ErrorMessage = "User is required")]
    public int UserId { get; set; }

    [StringLength(50, ErrorMessage = "Role cannot exceed 50 characters")]
    public string Role { get; set; } = "Member";
}

#endregion
