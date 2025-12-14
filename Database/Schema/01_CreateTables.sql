-- =====================================================
-- TICKETING SYSTEM DATABASE SCHEMA
-- Version: 1.0
-- Description: Complete schema for enterprise ticketing system
-- Security: Uses stored procedures for all CRUD operations
-- =====================================================

-- =====================================================
-- CORE TABLES
-- =====================================================

-- Roles Table
CREATE TABLE IF NOT EXISTS Roles (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(255),
    Permissions JSONB DEFAULT '{}',
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Users Table
CREATE TABLE IF NOT EXISTS Users (
    Id SERIAL PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    PasswordSalt VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    DisplayName VARCHAR(200),
    Phone VARCHAR(20),
    AvatarUrl VARCHAR(500),
    IsActive BOOLEAN DEFAULT TRUE,
    IsLocked BOOLEAN DEFAULT FALSE,
    FailedLoginAttempts INT DEFAULT 0,
    LastLoginAt TIMESTAMP WITH TIME ZONE,
    PasswordChangedAt TIMESTAMP WITH TIME ZONE,
    MustChangePassword BOOLEAN DEFAULT FALSE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CreatedBy INT REFERENCES Users(Id),
    UpdatedBy INT REFERENCES Users(Id)
);

-- User Roles Junction Table
CREATE TABLE IF NOT EXISTS UserRoles (
    Id SERIAL PRIMARY KEY,
    UserId INT NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    RoleId INT NOT NULL REFERENCES Roles(Id) ON DELETE CASCADE,
    AssignedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    AssignedBy INT REFERENCES Users(Id),
    UNIQUE(UserId, RoleId)
);

-- Teams Table
CREATE TABLE IF NOT EXISTS Teams (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description VARCHAR(500),
    ManagerId INT REFERENCES Users(Id),
    Email VARCHAR(255),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CreatedBy INT REFERENCES Users(Id),
    UpdatedBy INT REFERENCES Users(Id)
);

-- Team Members Junction Table
CREATE TABLE IF NOT EXISTS TeamMembers (
    Id SERIAL PRIMARY KEY,
    TeamId INT NOT NULL REFERENCES Teams(Id) ON DELETE CASCADE,
    UserId INT NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    Role VARCHAR(50) DEFAULT 'Member', -- Leader, Member
    JoinedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    AddedBy INT REFERENCES Users(Id),
    UNIQUE(TeamId, UserId)
);

-- =====================================================
-- TICKET CONFIGURATION TABLES
-- =====================================================

-- Priorities Table
CREATE TABLE IF NOT EXISTS Priorities (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(255),
    Level INT NOT NULL UNIQUE, -- 1=Critical, 2=High, 3=Medium, 4=Low
    Color VARCHAR(7) DEFAULT '#808080', -- Hex color code
    SlaResponseHours INT, -- Expected response time in hours
    SlaResolutionHours INT, -- Expected resolution time in hours
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Statuses Table
CREATE TABLE IF NOT EXISTS Statuses (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(255),
    DisplayOrder INT NOT NULL,
    Color VARCHAR(7) DEFAULT '#808080',
    IsDefault BOOLEAN DEFAULT FALSE,
    IsClosed BOOLEAN DEFAULT FALSE, -- Indicates if this status closes the ticket
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Categories Table
CREATE TABLE IF NOT EXISTS Categories (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    ParentId INT REFERENCES Categories(Id), -- For sub-categories
    DefaultTeamId INT REFERENCES Teams(Id), -- Auto-assign to this team
    DefaultPriorityId INT REFERENCES Priorities(Id),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CreatedBy INT REFERENCES Users(Id),
    UNIQUE(Name, ParentId)
);

-- =====================================================
-- TICKET TABLES
-- =====================================================

-- Main Tickets Table
CREATE TABLE IF NOT EXISTS Tickets (
    Id SERIAL PRIMARY KEY,
    TicketNumber VARCHAR(20) NOT NULL UNIQUE, -- e.g., TKT-2024-00001
    Title VARCHAR(255) NOT NULL,
    Description TEXT,
    CategoryId INT REFERENCES Categories(Id),
    PriorityId INT NOT NULL REFERENCES Priorities(Id),
    StatusId INT NOT NULL REFERENCES Statuses(Id),
    RequesterId INT NOT NULL REFERENCES Users(Id), -- Who created the ticket
    AssignedToId INT REFERENCES Users(Id), -- Individual assignee
    AssignedTeamId INT REFERENCES Teams(Id), -- Team assignment
    DueDate TIMESTAMP WITH TIME ZONE,
    ResolvedAt TIMESTAMP WITH TIME ZONE,
    ClosedAt TIMESTAMP WITH TIME ZONE,
    FirstResponseAt TIMESTAMP WITH TIME ZONE,
    SlaBreached BOOLEAN DEFAULT FALSE,
    Tags VARCHAR(500), -- Comma-separated tags
    Source VARCHAR(50) DEFAULT 'Web', -- Web, Email, API, Phone
    IsDeleted BOOLEAN DEFAULT FALSE,
    DeletedAt TIMESTAMP WITH TIME ZONE,
    DeletedBy INT REFERENCES Users(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CreatedBy INT REFERENCES Users(Id),
    UpdatedBy INT REFERENCES Users(Id)
);

-- Ticket Comments Table
CREATE TABLE IF NOT EXISTS TicketComments (
    Id SERIAL PRIMARY KEY,
    TicketId INT NOT NULL REFERENCES Tickets(Id) ON DELETE CASCADE,
    UserId INT NOT NULL REFERENCES Users(Id),
    Content TEXT NOT NULL,
    IsInternal BOOLEAN DEFAULT FALSE, -- Internal notes not visible to requester
    IsResolution BOOLEAN DEFAULT FALSE, -- Marks the resolution comment
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN DEFAULT FALSE,
    DeletedAt TIMESTAMP WITH TIME ZONE,
    DeletedBy INT REFERENCES Users(Id)
);

-- Ticket Attachments Table
CREATE TABLE IF NOT EXISTS TicketAttachments (
    Id SERIAL PRIMARY KEY,
    TicketId INT NOT NULL REFERENCES Tickets(Id) ON DELETE CASCADE,
    CommentId INT REFERENCES TicketComments(Id) ON DELETE CASCADE,
    FileName VARCHAR(255) NOT NULL,
    FileSize BIGINT NOT NULL,
    ContentType VARCHAR(100) NOT NULL,
    StoragePath VARCHAR(500) NOT NULL,
    UploadedBy INT NOT NULL REFERENCES Users(Id),
    UploadedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN DEFAULT FALSE,
    DeletedAt TIMESTAMP WITH TIME ZONE,
    DeletedBy INT REFERENCES Users(Id)
);

-- Ticket History Table (Audit Trail)
CREATE TABLE IF NOT EXISTS TicketHistory (
    Id SERIAL PRIMARY KEY,
    TicketId INT NOT NULL REFERENCES Tickets(Id) ON DELETE CASCADE,
    UserId INT NOT NULL REFERENCES Users(Id),
    Action VARCHAR(50) NOT NULL, -- Created, Updated, StatusChanged, Assigned, etc.
    FieldName VARCHAR(100),
    OldValue TEXT,
    NewValue TEXT,
    Description TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    IpAddress VARCHAR(45),
    UserAgent VARCHAR(500)
);

-- =====================================================
-- SECURITY & AUDIT TABLES
-- =====================================================

-- Audit Logs Table
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id SERIAL PRIMARY KEY,
    UserId INT REFERENCES Users(Id),
    Action VARCHAR(100) NOT NULL,
    EntityType VARCHAR(100),
    EntityId INT,
    OldValues JSONB,
    NewValues JSONB,
    IpAddress VARCHAR(45),
    UserAgent VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User Sessions Table (for security tracking)
CREATE TABLE IF NOT EXISTS UserSessions (
    Id SERIAL PRIMARY KEY,
    UserId INT NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    SessionToken VARCHAR(500) NOT NULL UNIQUE,
    RefreshToken VARCHAR(500),
    IpAddress VARCHAR(45),
    UserAgent VARCHAR(500),
    ExpiresAt TIMESTAMP WITH TIME ZONE NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    RevokedAt TIMESTAMP WITH TIME ZONE,
    IsRevoked BOOLEAN DEFAULT FALSE
);

-- =====================================================
-- REPORTING TABLES
-- =====================================================

-- Report Definitions Table
CREATE TABLE IF NOT EXISTS ReportDefinitions (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    ReportType VARCHAR(50) NOT NULL, -- TicketSummary, TeamPerformance, SLA, etc.
    QueryTemplate TEXT,
    Parameters JSONB,
    IsPublic BOOLEAN DEFAULT FALSE,
    CreatedBy INT NOT NULL REFERENCES Users(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Saved Reports Table
CREATE TABLE IF NOT EXISTS SavedReports (
    Id SERIAL PRIMARY KEY,
    ReportDefinitionId INT NOT NULL REFERENCES ReportDefinitions(Id) ON DELETE CASCADE,
    Name VARCHAR(100) NOT NULL,
    Parameters JSONB,
    ScheduleCron VARCHAR(100), -- Cron expression for scheduled reports
    EmailRecipients TEXT, -- Comma-separated emails
    LastRunAt TIMESTAMP WITH TIME ZONE,
    CreatedBy INT NOT NULL REFERENCES Users(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- INDEXES FOR PERFORMANCE
-- =====================================================

-- Users indexes
CREATE INDEX IF NOT EXISTS idx_users_email ON Users(Email);
CREATE INDEX IF NOT EXISTS idx_users_active ON Users(IsActive) WHERE IsActive = TRUE;

-- Tickets indexes
CREATE INDEX IF NOT EXISTS idx_tickets_number ON Tickets(TicketNumber);
CREATE INDEX IF NOT EXISTS idx_tickets_requester ON Tickets(RequesterId);
CREATE INDEX IF NOT EXISTS idx_tickets_assigned_user ON Tickets(AssignedToId);
CREATE INDEX IF NOT EXISTS idx_tickets_assigned_team ON Tickets(AssignedTeamId);
CREATE INDEX IF NOT EXISTS idx_tickets_status ON Tickets(StatusId);
CREATE INDEX IF NOT EXISTS idx_tickets_priority ON Tickets(PriorityId);
CREATE INDEX IF NOT EXISTS idx_tickets_category ON Tickets(CategoryId);
CREATE INDEX IF NOT EXISTS idx_tickets_created ON Tickets(CreatedAt DESC);
CREATE INDEX IF NOT EXISTS idx_tickets_not_deleted ON Tickets(IsDeleted) WHERE IsDeleted = FALSE;

-- Ticket comments indexes
CREATE INDEX IF NOT EXISTS idx_comments_ticket ON TicketComments(TicketId);
CREATE INDEX IF NOT EXISTS idx_comments_user ON TicketComments(UserId);

-- Ticket history indexes
CREATE INDEX IF NOT EXISTS idx_history_ticket ON TicketHistory(TicketId);
CREATE INDEX IF NOT EXISTS idx_history_created ON TicketHistory(CreatedAt DESC);

-- Audit logs indexes
CREATE INDEX IF NOT EXISTS idx_audit_user ON AuditLogs(UserId);
CREATE INDEX IF NOT EXISTS idx_audit_entity ON AuditLogs(EntityType, EntityId);
CREATE INDEX IF NOT EXISTS idx_audit_created ON AuditLogs(CreatedAt DESC);

-- Team members indexes
CREATE INDEX IF NOT EXISTS idx_teammembers_team ON TeamMembers(TeamId);
CREATE INDEX IF NOT EXISTS idx_teammembers_user ON TeamMembers(UserId);
