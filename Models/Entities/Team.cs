namespace TicketingASP.Models.Entities;

/// <summary>
/// Represents a support team
/// </summary>
public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ManagerId { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation properties
    public virtual User? Manager { get; set; }
    public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public virtual ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
}

/// <summary>
/// Junction table for Team-User many-to-many relationship
/// </summary>
public class TeamMember
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "Member"; // Leader, Member
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public int? AddedBy { get; set; }

    // Navigation properties
    public virtual Team Team { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
