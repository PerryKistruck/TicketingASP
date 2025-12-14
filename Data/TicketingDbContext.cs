using Microsoft.EntityFrameworkCore;
using TicketingASP.Models.Entities;

namespace TicketingASP.Data;

/// <summary>
/// Entity Framework Core database context for the ticketing system
/// </summary>
public class TicketingDbContext : DbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options) : base(options)
    {
    }

    // Core entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    // Ticket entities
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketHistory> TicketHistory => Set<TicketHistory>();

    // Configuration entities
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<Category> Categories => Set<Category>();

    // Audit entities
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names (PostgreSQL uses lowercase)
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<UserRole>().ToTable("userroles");
        modelBuilder.Entity<Team>().ToTable("teams");
        modelBuilder.Entity<TeamMember>().ToTable("teammembers");
        modelBuilder.Entity<Ticket>().ToTable("tickets");
        modelBuilder.Entity<TicketComment>().ToTable("ticketcomments");
        modelBuilder.Entity<TicketAttachment>().ToTable("ticketattachments");
        modelBuilder.Entity<TicketHistory>().ToTable("tickethistory");
        modelBuilder.Entity<Priority>().ToTable("priorities");
        modelBuilder.Entity<Status>().ToTable("statuses");
        modelBuilder.Entity<Category>().ToTable("categories");
        modelBuilder.Entity<AuditLog>().ToTable("auditlogs");
        modelBuilder.Entity<UserSession>().ToTable("usersessions");

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasColumnName("passwordhash").IsRequired();
            entity.Property(e => e.PasswordSalt).HasColumnName("passwordsalt").IsRequired();
            entity.Property(e => e.FirstName).HasColumnName("firstname").IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("lastname").IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).HasColumnName("displayname").HasMaxLength(200);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.AvatarUrl).HasColumnName("avatarurl").HasMaxLength(500);
            entity.Property(e => e.IsActive).HasColumnName("isactive").HasDefaultValue(true);
            entity.Property(e => e.IsLocked).HasColumnName("islocked").HasDefaultValue(false);
            entity.Property(e => e.FailedLoginAttempts).HasColumnName("failedloginattempts").HasDefaultValue(0);
            entity.Property(e => e.LastLoginAt).HasColumnName("lastloginat");
            entity.Property(e => e.PasswordChangedAt).HasColumnName("passwordchangedat");
            entity.Property(e => e.MustChangePassword).HasColumnName("mustchangepassword").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat");
            entity.Property(e => e.CreatedBy).HasColumnName("createdby");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedby");

            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Permissions).HasColumnName("permissions").HasColumnType("jsonb");
            entity.Property(e => e.IsActive).HasColumnName("isactive").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat");

            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure UserRole junction
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.RoleId).HasColumnName("roleid");
            entity.Property(e => e.AssignedAt).HasColumnName("assignedat");
            entity.Property(e => e.AssignedBy).HasColumnName("assignedby");

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        });

        // Configure Team entity
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.ManagerId).HasColumnName("managerid");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.IsActive).HasColumnName("isactive").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat");
            entity.Property(e => e.CreatedBy).HasColumnName("createdby");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedby");

            entity.HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure TeamMember junction
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TeamId).HasColumnName("teamid");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(50).HasDefaultValue("Member");
            entity.Property(e => e.JoinedAt).HasColumnName("joinedat");
            entity.Property(e => e.AddedBy).HasColumnName("addedby");

            entity.HasOne(e => e.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TeamId, e.UserId }).IsUnique();
        });

        // Configure Priority entity
        modelBuilder.Entity<Priority>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.Color).HasColumnName("color").HasMaxLength(7).HasDefaultValue("#808080");
            entity.Property(e => e.SlaResponseHours).HasColumnName("slaresponsehours");
            entity.Property(e => e.SlaResolutionHours).HasColumnName("slaresolutionhours");
            entity.Property(e => e.IsActive).HasColumnName("isactive").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Level).IsUnique();
        });

        // Configure Status entity
        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.DisplayOrder).HasColumnName("displayorder");
            entity.Property(e => e.Color).HasColumnName("color").HasMaxLength(7).HasDefaultValue("#808080");
            entity.Property(e => e.IsDefault).HasColumnName("isdefault").HasDefaultValue(false);
            entity.Property(e => e.IsClosed).HasColumnName("isclosed").HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasColumnName("isactive").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");

            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.ParentId).HasColumnName("parentid");
            entity.Property(e => e.DefaultTeamId).HasColumnName("defaultteamid");
            entity.Property(e => e.DefaultPriorityId).HasColumnName("defaultpriorityid");
            entity.Property(e => e.IsActive).HasColumnName("isactive").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat");
            entity.Property(e => e.CreatedBy).HasColumnName("createdby");

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.SubCategories)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DefaultTeam)
                .WithMany()
                .HasForeignKey(e => e.DefaultTeamId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.DefaultPriority)
                .WithMany()
                .HasForeignKey(e => e.DefaultPriorityId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.Name, e.ParentId }).IsUnique();
        });

        // Configure Ticket entity
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketNumber).HasColumnName("ticketnumber").IsRequired().HasMaxLength(20);
            entity.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CategoryId).HasColumnName("categoryid");
            entity.Property(e => e.PriorityId).HasColumnName("priorityid");
            entity.Property(e => e.StatusId).HasColumnName("statusid");
            entity.Property(e => e.RequesterId).HasColumnName("requesterid");
            entity.Property(e => e.AssignedToId).HasColumnName("assignedtoid");
            entity.Property(e => e.AssignedTeamId).HasColumnName("assignedteamid");
            entity.Property(e => e.DueDate).HasColumnName("duedate");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolvedat");
            entity.Property(e => e.ClosedAt).HasColumnName("closedat");
            entity.Property(e => e.FirstResponseAt).HasColumnName("firstresponseat");
            entity.Property(e => e.SlaBreached).HasColumnName("slabreached").HasDefaultValue(false);
            entity.Property(e => e.Tags).HasColumnName("tags").HasMaxLength(500);
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(50).HasDefaultValue("Web");
            entity.Property(e => e.IsDeleted).HasColumnName("isdeleted").HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).HasColumnName("deletedat");
            entity.Property(e => e.DeletedBy).HasColumnName("deletedby");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat");
            entity.Property(e => e.CreatedBy).HasColumnName("createdby");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedby");

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Tickets)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Priority)
                .WithMany(p => p.Tickets)
                .HasForeignKey(e => e.PriorityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Status)
                .WithMany(s => s.Tickets)
                .HasForeignKey(e => e.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Requester)
                .WithMany(u => u.RequestedTickets)
                .HasForeignKey(e => e.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedTo)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(e => e.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.AssignedTeam)
                .WithMany(t => t.AssignedTickets)
                .HasForeignKey(e => e.AssignedTeamId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.TicketNumber).IsUnique();
            entity.HasIndex(e => e.RequesterId);
            entity.HasIndex(e => e.AssignedToId);
            entity.HasIndex(e => e.AssignedTeamId);
            entity.HasIndex(e => e.StatusId);
            entity.HasIndex(e => e.PriorityId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure TicketComment entity
        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketId).HasColumnName("ticketid");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.IsInternal).HasColumnName("isinternal").HasDefaultValue(false);
            entity.Property(e => e.IsResolution).HasColumnName("isresolution").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat");
            entity.Property(e => e.IsDeleted).HasColumnName("isdeleted").HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).HasColumnName("deletedat");
            entity.Property(e => e.DeletedBy).HasColumnName("deletedby");

            entity.HasOne(e => e.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TicketId);
        });

        // Configure TicketAttachment entity
        modelBuilder.Entity<TicketAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketId).HasColumnName("ticketid");
            entity.Property(e => e.CommentId).HasColumnName("commentid");
            entity.Property(e => e.FileName).HasColumnName("filename").IsRequired().HasMaxLength(255);
            entity.Property(e => e.FileSize).HasColumnName("filesize");
            entity.Property(e => e.ContentType).HasColumnName("contenttype").IsRequired().HasMaxLength(100);
            entity.Property(e => e.StoragePath).HasColumnName("storagepath").IsRequired().HasMaxLength(500);
            entity.Property(e => e.UploadedBy).HasColumnName("uploadedby");
            entity.Property(e => e.UploadedAt).HasColumnName("uploadedat");
            entity.Property(e => e.IsDeleted).HasColumnName("isdeleted").HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).HasColumnName("deletedat");
            entity.Property(e => e.DeletedBy).HasColumnName("deletedby");

            entity.HasOne(e => e.Ticket)
                .WithMany(t => t.Attachments)
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Comment)
                .WithMany(c => c.Attachments)
                .HasForeignKey(e => e.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Uploader)
                .WithMany()
                .HasForeignKey(e => e.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure TicketHistory entity
        modelBuilder.Entity<TicketHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TicketId).HasColumnName("ticketid");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.Action).HasColumnName("action").IsRequired().HasMaxLength(50);
            entity.Property(e => e.FieldName).HasColumnName("fieldname").HasMaxLength(100);
            entity.Property(e => e.OldValue).HasColumnName("oldvalue");
            entity.Property(e => e.NewValue).HasColumnName("newvalue");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            entity.Property(e => e.IpAddress).HasColumnName("ipaddress").HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasColumnName("useragent").HasMaxLength(500);

            entity.HasOne(e => e.Ticket)
                .WithMany(t => t.History)
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.Action).HasColumnName("action").IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).HasColumnName("entitytype").HasMaxLength(100);
            entity.Property(e => e.EntityId).HasColumnName("entityid");
            entity.Property(e => e.OldValues).HasColumnName("oldvalues").HasColumnType("jsonb");
            entity.Property(e => e.NewValues).HasColumnName("newvalues").HasColumnType("jsonb");
            entity.Property(e => e.IpAddress).HasColumnName("ipaddress").HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasColumnName("useragent").HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure UserSession entity
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.SessionToken).HasColumnName("sessiontoken").IsRequired().HasMaxLength(500);
            entity.Property(e => e.RefreshToken).HasColumnName("refreshtoken").HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasColumnName("ipaddress").HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasColumnName("useragent").HasMaxLength(500);
            entity.Property(e => e.ExpiresAt).HasColumnName("expiresat");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            entity.Property(e => e.RevokedAt).HasColumnName("revokedat");
            entity.Property(e => e.IsRevoked).HasColumnName("isrevoked").HasDefaultValue(false);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.SessionToken).IsUnique();
        });
    }
}
