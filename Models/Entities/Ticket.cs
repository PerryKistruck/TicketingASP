namespace TicketingASP.Models.Entities;

/// <summary>
/// Represents a support ticket
/// </summary>
public class Ticket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public int PriorityId { get; set; }
    public int StatusId { get; set; }
    public int RequesterId { get; set; }
    public int? AssignedToId { get; set; }
    public int? AssignedTeamId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public bool SlaBreached { get; set; } = false;
    public string? Tags { get; set; }
    public string Source { get; set; } = "Web";
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation properties
    public virtual Category? Category { get; set; }
    public virtual Priority Priority { get; set; } = null!;
    public virtual Status Status { get; set; } = null!;
    public virtual User Requester { get; set; } = null!;
    public virtual User? AssignedTo { get; set; }
    public virtual Team? AssignedTeam { get; set; }
    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public virtual ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
}

/// <summary>
/// Represents a comment on a ticket
/// </summary>
public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false;
    public bool IsResolution { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }

    // Navigation properties
    public virtual Ticket Ticket { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}

/// <summary>
/// Represents a file attachment on a ticket
/// </summary>
public class TicketAttachment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int? CommentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }

    // Navigation properties
    public virtual Ticket Ticket { get; set; } = null!;
    public virtual TicketComment? Comment { get; set; }
    public virtual User Uploader { get; set; } = null!;
}

/// <summary>
/// Represents a history entry for ticket changes (audit trail)
/// </summary>
public class TicketHistory
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Navigation properties
    public virtual Ticket Ticket { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
