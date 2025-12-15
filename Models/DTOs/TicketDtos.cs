using System.ComponentModel.DataAnnotations;

namespace TicketingASP.Models.DTOs;

#region Ticket DTOs

/// <summary>
/// DTO for creating a new ticket
/// </summary>
public class CreateTicketDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 255 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(10000, ErrorMessage = "Description cannot exceed 10000 characters")]
    public string? Description { get; set; }

    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Priority is required")]
    public int PriorityId { get; set; }

    public int? AssignedToId { get; set; }
    public int? AssignedTeamId { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// DTO for updating a ticket
/// </summary>
public class UpdateTicketDto
{
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 255 characters")]
    public string? Title { get; set; }

    [StringLength(10000, ErrorMessage = "Description cannot exceed 10000 characters")]
    public string? Description { get; set; }

    public int? CategoryId { get; set; }
    public int? PriorityId { get; set; }
    public int? StatusId { get; set; }
    public int? AssignedToId { get; set; }
    public int? AssignedTeamId { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// DTO for ticket list display
/// </summary>
public class TicketListDto
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public string PriorityColor { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public int? AssignedTeamId { get; set; }
    public string? AssignedTeamName { get; set; }
    public string? TeamName { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long TotalCount { get; set; }  // Used for pagination - returned from stored procedure
}

/// <summary>
/// DTO for ticket details display
/// </summary>
public class TicketDetailDto
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public string PriorityColor { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
    public int RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public int? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public int? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public int? AssignedTeamId { get; set; }
    public string? AssignedTeamName { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public bool SlaBreached { get; set; }
    public string? Tags { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<TicketCommentDto> Comments { get; set; } = new();
    public List<TicketHistoryDto> History { get; set; } = new();
}

/// <summary>
/// DTO for adding a comment to a ticket
/// </summary>
public class AddCommentDto
{
    [Required(ErrorMessage = "Comment content is required")]
    [StringLength(10000, MinimumLength = 1, ErrorMessage = "Comment must be between 1 and 10000 characters")]
    public string Content { get; set; } = string.Empty;

    public bool IsInternal { get; set; } = false;
    public bool IsResolution { get; set; } = false;
}

/// <summary>
/// DTO for ticket comment display
/// </summary>
public class TicketCommentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public bool IsResolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for ticket history display
/// </summary>
public class TicketHistoryDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ChangedAt { get; set; }
}

#endregion

#region Filter DTOs

/// <summary>
/// DTO for ticket filtering and pagination
/// </summary>
public class TicketFilterDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public int? StatusId { get; set; }
    public int? PriorityId { get; set; }
    public int? CategoryId { get; set; }
    public int? AssignedToId { get; set; }
    public int? AssignedTeamId { get; set; }
    public int? RequesterId { get; set; }
    public int? CreatedById { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool IncludeClosed { get; set; } = false;
}

#endregion

#region Lookup DTOs

/// <summary>
/// Generic lookup item for dropdowns
/// </summary>
public class LookupItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}

/// <summary>
/// Category lookup with parent info
/// </summary>
public class CategoryLookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string DisplayName => ParentName != null ? $"{ParentName} > {Name}" : Name;
    public List<CategoryLookupDto> SubCategories { get; set; } = new();
}

/// <summary>
/// Agent lookup for assignment
/// </summary>
public class AgentLookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// DTO containing all lookup data for ticket forms
/// </summary>
public class TicketFormLookupsDto
{
    public List<LookupItemDto> Priorities { get; set; } = new();
    public List<LookupItemDto> Statuses { get; set; } = new();
    public List<CategoryLookupDto> Categories { get; set; } = new();
    public List<LookupItemDto> Teams { get; set; } = new();
    public List<AgentLookupDto> Agents { get; set; } = new();
}

#endregion
