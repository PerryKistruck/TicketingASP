namespace TicketingASP.Models.Entities;

/// <summary>
/// Represents ticket priority levels
/// </summary>
public class Priority
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Level { get; set; } // 1=Critical, 2=High, 3=Medium, 4=Low
    public string Color { get; set; } = "#808080";
    public int? SlaResponseHours { get; set; }
    public int? SlaResolutionHours { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

/// <summary>
/// Represents ticket status values
/// </summary>
public class Status
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string Color { get; set; } = "#808080";
    public bool IsDefault { get; set; } = false;
    public bool IsClosed { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

/// <summary>
/// Represents ticket categories
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int? DefaultTeamId { get; set; }
    public int? DefaultPriorityId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    // Navigation properties
    public virtual Category? Parent { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public virtual Team? DefaultTeam { get; set; }
    public virtual Priority? DefaultPriority { get; set; }
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
