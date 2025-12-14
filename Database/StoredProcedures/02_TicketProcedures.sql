-- =====================================================
-- TICKET MANAGEMENT STORED PROCEDURES
-- Security: All ticket operations go through stored procedures
-- =====================================================

-- =====================================================
-- GENERATE TICKET NUMBER
-- =====================================================
CREATE OR REPLACE FUNCTION fn_generate_ticket_number()
RETURNS VARCHAR(20) AS $$
DECLARE
    v_year VARCHAR(4);
    v_seq INT;
    v_ticket_number VARCHAR(20);
BEGIN
    v_year := TO_CHAR(CURRENT_DATE, 'YYYY');
    
    SELECT COALESCE(MAX(CAST(SUBSTRING(TicketNumber FROM 10) AS INT)), 0) + 1 INTO v_seq
    FROM Tickets
    WHERE TicketNumber LIKE 'TKT-' || v_year || '-%';
    
    v_ticket_number := 'TKT-' || v_year || '-' || LPAD(v_seq::TEXT, 5, '0');
    
    RETURN v_ticket_number;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- CREATE TICKET
-- =====================================================
CREATE OR REPLACE FUNCTION sp_ticket_create(
    p_title VARCHAR(255),
    p_description TEXT,
    p_category_id INT,
    p_priority_id INT,
    p_requester_id INT,
    p_assigned_to_id INT DEFAULT NULL,
    p_assigned_team_id INT DEFAULT NULL,
    p_due_date TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_tags VARCHAR(500) DEFAULT NULL,
    p_source VARCHAR(50) DEFAULT 'Web',
    p_created_by INT DEFAULT NULL,
    p_ip_address VARCHAR(45) DEFAULT NULL
)
RETURNS TABLE(
    ticket_id INT,
    ticket_number VARCHAR(20),
    success BOOLEAN,
    message VARCHAR(255)
) AS $$
DECLARE
    v_ticket_id INT;
    v_ticket_number VARCHAR(20);
    v_default_status_id INT;
BEGIN
    -- Get default status
    SELECT Id INTO v_default_status_id FROM Statuses WHERE IsDefault = TRUE LIMIT 1;
    IF v_default_status_id IS NULL THEN
        SELECT Id INTO v_default_status_id FROM Statuses ORDER BY DisplayOrder LIMIT 1;
    END IF;

    -- Generate ticket number
    v_ticket_number := fn_generate_ticket_number();

    -- Create the ticket
    INSERT INTO Tickets (
        TicketNumber, Title, Description, CategoryId, PriorityId, StatusId,
        RequesterId, AssignedToId, AssignedTeamId, DueDate, Tags, Source, CreatedBy
    )
    VALUES (
        v_ticket_number, p_title, p_description, p_category_id, p_priority_id, v_default_status_id,
        p_requester_id, p_assigned_to_id, p_assigned_team_id, p_due_date, p_tags, p_source, p_created_by
    )
    RETURNING Id INTO v_ticket_id;

    -- Create history entry
    INSERT INTO TicketHistory (TicketId, UserId, Action, Description, IpAddress)
    VALUES (v_ticket_id, COALESCE(p_created_by, p_requester_id), 'Created', 'Ticket created', p_ip_address);

    -- Log the action
    INSERT INTO AuditLogs (UserId, Action, EntityType, EntityId, NewValues, IpAddress)
    VALUES (COALESCE(p_created_by, p_requester_id), 'CREATE', 'Ticket', v_ticket_id,
            jsonb_build_object('ticketNumber', v_ticket_number, 'title', p_title), p_ip_address);

    RETURN QUERY SELECT v_ticket_id, v_ticket_number, TRUE, 'Ticket created successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- GET TICKET BY ID
-- =====================================================
CREATE OR REPLACE FUNCTION sp_ticket_get_by_id(p_ticket_id INT)
RETURNS TABLE(
    id INT,
    ticket_number VARCHAR(20),
    title VARCHAR(255),
    description TEXT,
    category_id INT,
    category_name VARCHAR(100),
    priority_id INT,
    priority_name VARCHAR(50),
    priority_color VARCHAR(7),
    status_id INT,
    status_name VARCHAR(50),
    status_color VARCHAR(7),
    requester_id INT,
    requester_name VARCHAR(200),
    requester_email VARCHAR(255),
    assigned_to_id INT,
    assigned_to_name VARCHAR(200),
    assigned_team_id INT,
    assigned_team_name VARCHAR(100),
    due_date TIMESTAMP WITH TIME ZONE,
    resolved_at TIMESTAMP WITH TIME ZONE,
    closed_at TIMESTAMP WITH TIME ZONE,
    first_response_at TIMESTAMP WITH TIME ZONE,
    sla_breached BOOLEAN,
    tags VARCHAR(500),
    source VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT t.Id, t.TicketNumber, t.Title, t.Description,
           t.CategoryId, c.Name AS category_name,
           t.PriorityId, p.Name AS priority_name, p.Color AS priority_color,
           t.StatusId, s.Name AS status_name, s.Color AS status_color,
           t.RequesterId, ur.DisplayName AS requester_name, ur.Email AS requester_email,
           t.AssignedToId, ua.DisplayName AS assigned_to_name,
           t.AssignedTeamId, tm.Name AS assigned_team_name,
           t.DueDate, t.ResolvedAt, t.ClosedAt, t.FirstResponseAt, t.SlaBreached,
           t.Tags, t.Source, t.CreatedAt, t.UpdatedAt
    FROM Tickets t
    LEFT JOIN Categories c ON t.CategoryId = c.Id
    LEFT JOIN Priorities p ON t.PriorityId = p.Id
    LEFT JOIN Statuses s ON t.StatusId = s.Id
    LEFT JOIN Users ur ON t.RequesterId = ur.Id
    LEFT JOIN Users ua ON t.AssignedToId = ua.Id
    LEFT JOIN Teams tm ON t.AssignedTeamId = tm.Id
    WHERE t.Id = p_ticket_id AND t.IsDeleted = FALSE;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- UPDATE TICKET
-- =====================================================
CREATE OR REPLACE FUNCTION sp_ticket_update(
    p_ticket_id INT,
    p_title VARCHAR(255) DEFAULT NULL,
    p_description TEXT DEFAULT NULL,
    p_category_id INT DEFAULT NULL,
    p_priority_id INT DEFAULT NULL,
    p_status_id INT DEFAULT NULL,
    p_assigned_to_id INT DEFAULT NULL,
    p_assigned_team_id INT DEFAULT NULL,
    p_due_date TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_tags VARCHAR(500) DEFAULT NULL,
    p_updated_by INT DEFAULT NULL,
    p_ip_address VARCHAR(45) DEFAULT NULL
)
RETURNS TABLE(success BOOLEAN, message VARCHAR(255)) AS $$
DECLARE
    v_old_record RECORD;
    v_status_is_closed BOOLEAN;
BEGIN
    -- Get old values
    SELECT * INTO v_old_record FROM Tickets WHERE Id = p_ticket_id AND IsDeleted = FALSE;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'Ticket not found'::VARCHAR(255);
        RETURN;
    END IF;

    -- Check if status is changing to closed
    IF p_status_id IS NOT NULL AND p_status_id != v_old_record.StatusId THEN
        SELECT IsClosed INTO v_status_is_closed FROM Statuses WHERE Id = p_status_id;
    END IF;

    -- Update the ticket
    UPDATE Tickets
    SET Title = COALESCE(p_title, Title),
        Description = COALESCE(p_description, Description),
        CategoryId = COALESCE(p_category_id, CategoryId),
        PriorityId = COALESCE(p_priority_id, PriorityId),
        StatusId = COALESCE(p_status_id, StatusId),
        AssignedToId = p_assigned_to_id,
        AssignedTeamId = p_assigned_team_id,
        DueDate = p_due_date,
        Tags = COALESCE(p_tags, Tags),
        UpdatedAt = CURRENT_TIMESTAMP,
        UpdatedBy = p_updated_by,
        ClosedAt = CASE WHEN v_status_is_closed = TRUE THEN CURRENT_TIMESTAMP ELSE ClosedAt END,
        ResolvedAt = CASE 
            WHEN p_status_id IS NOT NULL AND (SELECT Name FROM Statuses WHERE Id = p_status_id) = 'Resolved' 
            THEN CURRENT_TIMESTAMP 
            ELSE ResolvedAt 
        END
    WHERE Id = p_ticket_id;

    -- Record field changes in history
    IF p_title IS NOT NULL AND p_title != v_old_record.Title THEN
        INSERT INTO TicketHistory (TicketId, UserId, Action, FieldName, OldValue, NewValue, IpAddress)
        VALUES (p_ticket_id, p_updated_by, 'Updated', 'Title', v_old_record.Title, p_title, p_ip_address);
    END IF;

    IF p_priority_id IS NOT NULL AND p_priority_id != v_old_record.PriorityId THEN
        INSERT INTO TicketHistory (TicketId, UserId, Action, FieldName, OldValue, NewValue, IpAddress)
        VALUES (p_ticket_id, p_updated_by, 'PriorityChanged', 'Priority', 
                (SELECT Name FROM Priorities WHERE Id = v_old_record.PriorityId),
                (SELECT Name FROM Priorities WHERE Id = p_priority_id), p_ip_address);
    END IF;

    IF p_status_id IS NOT NULL AND p_status_id != v_old_record.StatusId THEN
        INSERT INTO TicketHistory (TicketId, UserId, Action, FieldName, OldValue, NewValue, IpAddress)
        VALUES (p_ticket_id, p_updated_by, 'StatusChanged', 'Status',
                (SELECT Name FROM Statuses WHERE Id = v_old_record.StatusId),
                (SELECT Name FROM Statuses WHERE Id = p_status_id), p_ip_address);
    END IF;

    IF p_assigned_to_id IS DISTINCT FROM v_old_record.AssignedToId THEN
        INSERT INTO TicketHistory (TicketId, UserId, Action, FieldName, OldValue, NewValue, IpAddress)
        VALUES (p_ticket_id, p_updated_by, 'Assigned', 'AssignedTo',
                (SELECT DisplayName FROM Users WHERE Id = v_old_record.AssignedToId),
                (SELECT DisplayName FROM Users WHERE Id = p_assigned_to_id), p_ip_address);
    END IF;

    RETURN QUERY SELECT TRUE, 'Ticket updated successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- DELETE TICKET (Soft Delete)
-- =====================================================
CREATE OR REPLACE FUNCTION sp_ticket_delete(
    p_ticket_id INT,
    p_deleted_by INT,
    p_ip_address VARCHAR(45) DEFAULT NULL
)
RETURNS TABLE(success BOOLEAN, message VARCHAR(255)) AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Tickets WHERE Id = p_ticket_id AND IsDeleted = FALSE) THEN
        RETURN QUERY SELECT FALSE, 'Ticket not found'::VARCHAR(255);
        RETURN;
    END IF;

    UPDATE Tickets
    SET IsDeleted = TRUE,
        DeletedAt = CURRENT_TIMESTAMP,
        DeletedBy = p_deleted_by,
        UpdatedAt = CURRENT_TIMESTAMP
    WHERE Id = p_ticket_id;

    -- Log the action
    INSERT INTO AuditLogs (UserId, Action, EntityType, EntityId, IpAddress)
    VALUES (p_deleted_by, 'DELETE', 'Ticket', p_ticket_id, p_ip_address);

    RETURN QUERY SELECT TRUE, 'Ticket deleted successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- LIST TICKETS WITH PAGINATION AND FILTERS
-- =====================================================
CREATE OR REPLACE FUNCTION sp_ticket_list(
    p_page_number INT DEFAULT 1,
    p_page_size INT DEFAULT 20,
    p_search VARCHAR(100) DEFAULT NULL,
    p_status_id INT DEFAULT NULL,
    p_priority_id INT DEFAULT NULL,
    p_category_id INT DEFAULT NULL,
    p_assigned_to_id INT DEFAULT NULL,
    p_assigned_team_id INT DEFAULT NULL,
    p_requester_id INT DEFAULT NULL,
    p_date_from TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_date_to TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_include_closed BOOLEAN DEFAULT FALSE,
    p_user_id INT DEFAULT NULL,
    p_user_role VARCHAR(50) DEFAULT 'User'
)
RETURNS TABLE(
    id INT,
    ticket_number VARCHAR(20),
    title VARCHAR(255),
    priority_name VARCHAR(50),
    priority_color VARCHAR(7),
    status_name VARCHAR(50),
    status_color VARCHAR(7),
    requester_name VARCHAR(200),
    assigned_to_name VARCHAR(200),
    team_name VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE,
    total_count BIGINT
) AS $$
DECLARE
    v_offset INT;
    v_total BIGINT;
BEGIN
    v_offset := (p_page_number - 1) * p_page_size;

    -- Get total count
    SELECT COUNT(*) INTO v_total
    FROM Tickets t
    WHERE t.IsDeleted = FALSE
      AND (p_search IS NULL OR t.Title ILIKE '%' || p_search || '%' OR t.TicketNumber ILIKE '%' || p_search || '%')
      AND (p_status_id IS NULL OR t.StatusId = p_status_id)
      AND (p_priority_id IS NULL OR t.PriorityId = p_priority_id)
      AND (p_category_id IS NULL OR t.CategoryId = p_category_id)
      AND (p_assigned_to_id IS NULL OR t.AssignedToId = p_assigned_to_id)
      AND (p_assigned_team_id IS NULL OR t.AssignedTeamId = p_assigned_team_id)
      AND (p_requester_id IS NULL OR t.RequesterId = p_requester_id)
      AND (p_date_from IS NULL OR t.CreatedAt >= p_date_from)
      AND (p_date_to IS NULL OR t.CreatedAt <= p_date_to)
      AND (p_include_closed = TRUE OR t.StatusId NOT IN (SELECT Id FROM Statuses WHERE IsClosed = TRUE))
      AND (
          p_user_role IN ('Administrator', 'Manager', 'Agent')
          OR t.RequesterId = p_user_id
          OR t.AssignedToId = p_user_id
      );

    RETURN QUERY
    SELECT t.Id, t.TicketNumber, t.Title,
           p.Name AS priority_name, p.Color AS priority_color,
           s.Name AS status_name, s.Color AS status_color,
           ur.DisplayName AS requester_name,
           ua.DisplayName AS assigned_to_name,
           tm.Name AS team_name,
           t.CreatedAt, t.UpdatedAt,
           v_total AS total_count
    FROM Tickets t
    LEFT JOIN Priorities p ON t.PriorityId = p.Id
    LEFT JOIN Statuses s ON t.StatusId = s.Id
    LEFT JOIN Users ur ON t.RequesterId = ur.Id
    LEFT JOIN Users ua ON t.AssignedToId = ua.Id
    LEFT JOIN Teams tm ON t.AssignedTeamId = tm.Id
    WHERE t.IsDeleted = FALSE
      AND (p_search IS NULL OR t.Title ILIKE '%' || p_search || '%' OR t.TicketNumber ILIKE '%' || p_search || '%')
      AND (p_status_id IS NULL OR t.StatusId = p_status_id)
      AND (p_priority_id IS NULL OR t.PriorityId = p_priority_id)
      AND (p_category_id IS NULL OR t.CategoryId = p_category_id)
      AND (p_assigned_to_id IS NULL OR t.AssignedToId = p_assigned_to_id)
      AND (p_assigned_team_id IS NULL OR t.AssignedTeamId = p_assigned_team_id)
      AND (p_requester_id IS NULL OR t.RequesterId = p_requester_id)
      AND (p_date_from IS NULL OR t.CreatedAt >= p_date_from)
      AND (p_date_to IS NULL OR t.CreatedAt <= p_date_to)
      AND (p_include_closed = TRUE OR t.StatusId NOT IN (SELECT Id FROM Statuses WHERE IsClosed = TRUE))
      AND (
          p_user_role IN ('Administrator', 'Manager', 'Agent')
          OR t.RequesterId = p_user_id
          OR t.AssignedToId = p_user_id
      )
    ORDER BY 
        CASE p.Level WHEN 1 THEN 1 WHEN 2 THEN 2 WHEN 3 THEN 3 ELSE 4 END,
        t.CreatedAt DESC
    LIMIT p_page_size OFFSET v_offset;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- ADD TICKET COMMENT
-- =====================================================
CREATE OR REPLACE FUNCTION sp_ticket_add_comment(
    p_ticket_id INT,
    p_user_id INT,
    p_content TEXT,
    p_is_internal BOOLEAN DEFAULT FALSE,
    p_is_resolution BOOLEAN DEFAULT FALSE,
    p_ip_address VARCHAR(45) DEFAULT NULL
)
RETURNS TABLE(
    comment_id INT,
    success BOOLEAN,
    message VARCHAR(255)
) AS $$
DECLARE
    v_comment_id INT;
    v_ticket_requester_id INT;
BEGIN
    -- Validate ticket exists
    SELECT RequesterId INTO v_ticket_requester_id FROM Tickets WHERE Id = p_ticket_id AND IsDeleted = FALSE;
    IF NOT FOUND THEN
        RETURN QUERY SELECT NULL::INT, FALSE, 'Ticket not found'::VARCHAR(255);
        RETURN;
    END IF;

    -- Create comment
    INSERT INTO TicketComments (TicketId, UserId, Content, IsInternal, IsResolution)
    VALUES (p_ticket_id, p_user_id, p_content, p_is_internal, p_is_resolution)
    RETURNING Id INTO v_comment_id;

    -- Update first response time if this is the first agent response
    IF p_user_id != v_ticket_requester_id THEN
        UPDATE Tickets
        SET FirstResponseAt = COALESCE(FirstResponseAt, CURRENT_TIMESTAMP),
            UpdatedAt = CURRENT_TIMESTAMP
        WHERE Id = p_ticket_id AND FirstResponseAt IS NULL;
    END IF;

    -- Update ticket timestamp
    UPDATE Tickets SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = p_ticket_id;

    -- Record in history
    INSERT INTO TicketHistory (TicketId, UserId, Action, Description, IpAddress)
    VALUES (p_ticket_id, p_user_id, 
            CASE WHEN p_is_internal THEN 'InternalNote' ELSE 'Comment' END,
            'Comment added', p_ip_address);

    RETURN QUERY SELECT v_comment_id, TRUE, 'Comment added successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- GET TICKET COMMENTS
-- =====================================================
CREATE OR REPLACE FUNCTION sp_ticket_get_comments(
    p_ticket_id INT,
    p_include_internal BOOLEAN DEFAULT FALSE
)
RETURNS TABLE(
    id INT,
    user_id INT,
    user_name VARCHAR(200),
    user_avatar VARCHAR(500),
    content TEXT,
    is_internal BOOLEAN,
    is_resolution BOOLEAN,
    created_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT c.Id, c.UserId, u.DisplayName, u.AvatarUrl, c.Content, 
           c.IsInternal, c.IsResolution, c.CreatedAt, c.UpdatedAt
    FROM TicketComments c
    JOIN Users u ON c.UserId = u.Id
    WHERE c.TicketId = p_ticket_id 
      AND c.IsDeleted = FALSE
      AND (p_include_internal = TRUE OR c.IsInternal = FALSE)
    ORDER BY c.CreatedAt ASC;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- GET TICKET HISTORY
-- =====================================================
CREATE OR REPLACE FUNCTION sp_ticket_get_history(p_ticket_id INT)
RETURNS TABLE(
    id INT,
    user_id INT,
    user_name VARCHAR(200),
    action VARCHAR(50),
    field_name VARCHAR(100),
    old_value TEXT,
    new_value TEXT,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT h.Id, h.UserId, u.DisplayName, h.Action, h.FieldName, 
           h.OldValue, h.NewValue, h.Description, h.CreatedAt
    FROM TicketHistory h
    JOIN Users u ON h.UserId = u.Id
    WHERE h.TicketId = p_ticket_id
    ORDER BY h.CreatedAt DESC;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
