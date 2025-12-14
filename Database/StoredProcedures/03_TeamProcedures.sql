-- =====================================================
-- TEAM MANAGEMENT STORED PROCEDURES
-- =====================================================

-- =====================================================
-- CREATE TEAM
-- =====================================================
CREATE OR REPLACE FUNCTION sp_team_create(
    p_name VARCHAR(100),
    p_description VARCHAR(500) DEFAULT NULL,
    p_manager_id INT DEFAULT NULL,
    p_email VARCHAR(255) DEFAULT NULL,
    p_created_by INT DEFAULT NULL
)
RETURNS TABLE(
    team_id INT,
    success BOOLEAN,
    message VARCHAR(255)
) AS $$
DECLARE
    v_team_id INT;
BEGIN
    -- Check if team name exists
    IF EXISTS (SELECT 1 FROM Teams WHERE Name = p_name) THEN
        RETURN QUERY SELECT NULL::INT, FALSE, 'Team name already exists'::VARCHAR(255);
        RETURN;
    END IF;

    INSERT INTO Teams (Name, Description, ManagerId, Email, CreatedBy)
    VALUES (p_name, p_description, p_manager_id, p_email, p_created_by)
    RETURNING Id INTO v_team_id;

    -- Add manager as team member if specified
    IF p_manager_id IS NOT NULL THEN
        INSERT INTO TeamMembers (TeamId, UserId, Role, AddedBy)
        VALUES (v_team_id, p_manager_id, 'Leader', p_created_by);
    END IF;

    -- Log the action
    INSERT INTO AuditLogs (UserId, Action, EntityType, EntityId, NewValues)
    VALUES (p_created_by, 'CREATE', 'Team', v_team_id, jsonb_build_object('name', p_name));

    RETURN QUERY SELECT v_team_id, TRUE, 'Team created successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- GET TEAM BY ID
-- =====================================================
CREATE OR REPLACE FUNCTION sp_team_get_by_id(p_team_id INT)
RETURNS TABLE(
    id INT,
    name VARCHAR(100),
    description VARCHAR(500),
    manager_id INT,
    manager_name VARCHAR(200),
    email VARCHAR(255),
    is_active BOOLEAN,
    member_count BIGINT,
    created_at TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT t.Id, t.Name, t.Description, t.ManagerId, u.DisplayName AS manager_name,
           t.Email, t.IsActive,
           (SELECT COUNT(*) FROM TeamMembers WHERE TeamId = t.Id) AS member_count,
           t.CreatedAt
    FROM Teams t
    LEFT JOIN Users u ON t.ManagerId = u.Id
    WHERE t.Id = p_team_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- UPDATE TEAM
-- =====================================================
CREATE OR REPLACE FUNCTION sp_team_update(
    p_team_id INT,
    p_name VARCHAR(100) DEFAULT NULL,
    p_description VARCHAR(500) DEFAULT NULL,
    p_manager_id INT DEFAULT NULL,
    p_email VARCHAR(255) DEFAULT NULL,
    p_is_active BOOLEAN DEFAULT NULL,
    p_updated_by INT DEFAULT NULL
)
RETURNS TABLE(success BOOLEAN, message VARCHAR(255)) AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Teams WHERE Id = p_team_id) THEN
        RETURN QUERY SELECT FALSE, 'Team not found'::VARCHAR(255);
        RETURN;
    END IF;

    UPDATE Teams
    SET Name = COALESCE(p_name, Name),
        Description = COALESCE(p_description, Description),
        ManagerId = COALESCE(p_manager_id, ManagerId),
        Email = COALESCE(p_email, Email),
        IsActive = COALESCE(p_is_active, IsActive),
        UpdatedAt = CURRENT_TIMESTAMP,
        UpdatedBy = p_updated_by
    WHERE Id = p_team_id;

    RETURN QUERY SELECT TRUE, 'Team updated successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- DELETE TEAM (Soft Delete)
-- =====================================================
CREATE OR REPLACE FUNCTION sp_team_delete(
    p_team_id INT,
    p_deleted_by INT
)
RETURNS TABLE(success BOOLEAN, message VARCHAR(255)) AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Teams WHERE Id = p_team_id) THEN
        RETURN QUERY SELECT FALSE, 'Team not found'::VARCHAR(255);
        RETURN;
    END IF;

    UPDATE Teams
    SET IsActive = FALSE,
        UpdatedAt = CURRENT_TIMESTAMP,
        UpdatedBy = p_deleted_by
    WHERE Id = p_team_id;

    INSERT INTO AuditLogs (UserId, Action, EntityType, EntityId)
    VALUES (p_deleted_by, 'DELETE', 'Team', p_team_id);

    RETURN QUERY SELECT TRUE, 'Team deactivated successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- LIST TEAMS
-- =====================================================
CREATE OR REPLACE FUNCTION sp_team_list(
    p_page_number INT DEFAULT 1,
    p_page_size INT DEFAULT 20,
    p_search VARCHAR(100) DEFAULT NULL,
    p_is_active BOOLEAN DEFAULT TRUE
)
RETURNS TABLE(
    id INT,
    name VARCHAR(100),
    description VARCHAR(500),
    manager_name VARCHAR(200),
    email VARCHAR(255),
    member_count BIGINT,
    is_active BOOLEAN,
    created_at TIMESTAMP WITH TIME ZONE,
    total_count BIGINT
) AS $$
DECLARE
    v_offset INT;
    v_total BIGINT;
BEGIN
    v_offset := (p_page_number - 1) * p_page_size;

    SELECT COUNT(*) INTO v_total
    FROM Teams t
    WHERE (p_search IS NULL OR t.Name ILIKE '%' || p_search || '%')
      AND (p_is_active IS NULL OR t.IsActive = p_is_active);

    RETURN QUERY
    SELECT t.Id, t.Name, t.Description, u.DisplayName AS manager_name, t.Email,
           (SELECT COUNT(*) FROM TeamMembers WHERE TeamId = t.Id) AS member_count,
           t.IsActive, t.CreatedAt, v_total AS total_count
    FROM Teams t
    LEFT JOIN Users u ON t.ManagerId = u.Id
    WHERE (p_search IS NULL OR t.Name ILIKE '%' || p_search || '%')
      AND (p_is_active IS NULL OR t.IsActive = p_is_active)
    ORDER BY t.Name
    LIMIT p_page_size OFFSET v_offset;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- ADD TEAM MEMBER
-- =====================================================
CREATE OR REPLACE FUNCTION sp_team_add_member(
    p_team_id INT,
    p_user_id INT,
    p_role VARCHAR(50) DEFAULT 'Member',
    p_added_by INT DEFAULT NULL
)
RETURNS TABLE(success BOOLEAN, message VARCHAR(255)) AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Teams WHERE Id = p_team_id AND IsActive = TRUE) THEN
        RETURN QUERY SELECT FALSE, 'Team not found or inactive'::VARCHAR(255);
        RETURN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = p_user_id AND IsActive = TRUE) THEN
        RETURN QUERY SELECT FALSE, 'User not found or inactive'::VARCHAR(255);
        RETURN;
    END IF;

    INSERT INTO TeamMembers (TeamId, UserId, Role, AddedBy)
    VALUES (p_team_id, p_user_id, p_role, p_added_by)
    ON CONFLICT (TeamId, UserId) DO UPDATE SET Role = p_role;

    RETURN QUERY SELECT TRUE, 'Team member added successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- REMOVE TEAM MEMBER
-- =====================================================
CREATE OR REPLACE FUNCTION sp_team_remove_member(
    p_team_id INT,
    p_user_id INT
)
RETURNS TABLE(success BOOLEAN, message VARCHAR(255)) AS $$
BEGIN
    DELETE FROM TeamMembers WHERE TeamId = p_team_id AND UserId = p_user_id;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'Team member not found'::VARCHAR(255);
        RETURN;
    END IF;

    RETURN QUERY SELECT TRUE, 'Team member removed successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- GET TEAM MEMBERS
-- =====================================================
CREATE OR REPLACE FUNCTION sp_team_get_members(p_team_id INT)
RETURNS TABLE(
    user_id INT,
    email VARCHAR(255),
    display_name VARCHAR(200),
    role VARCHAR(50),
    avatar_url VARCHAR(500),
    joined_at TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT u.Id, u.Email, u.DisplayName, tm.Role, u.AvatarUrl, tm.JoinedAt
    FROM TeamMembers tm
    JOIN Users u ON tm.UserId = u.Id
    WHERE tm.TeamId = p_team_id AND u.IsActive = TRUE
    ORDER BY 
        CASE tm.Role WHEN 'Leader' THEN 1 ELSE 2 END,
        u.DisplayName;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- GET USER TEAMS
-- =====================================================
CREATE OR REPLACE FUNCTION sp_user_get_teams(p_user_id INT)
RETURNS TABLE(
    team_id INT,
    team_name VARCHAR(100),
    role VARCHAR(50),
    member_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT t.Id, t.Name, tm.Role,
           (SELECT COUNT(*) FROM TeamMembers WHERE TeamId = t.Id) AS member_count
    FROM TeamMembers tm
    JOIN Teams t ON tm.TeamId = t.Id
    WHERE tm.UserId = p_user_id AND t.IsActive = TRUE
    ORDER BY t.Name;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
