-- =====================================================
-- USER MANAGEMENT STORED PROCEDURES
-- Security: All user operations go through stored procedures
-- =====================================================

-- =====================================================
-- CREATE USER
-- =====================================================
CREATE OR REPLACE FUNCTION sp_user_create(
    p_email VARCHAR(255),
    p_password_hash VARCHAR(255),
    p_password_salt VARCHAR(255),
    p_first_name VARCHAR(100),
    p_last_name VARCHAR(100),
    p_phone VARCHAR(20) DEFAULT NULL,
    p_created_by INT DEFAULT NULL
)
RETURNS TABLE(
    user_id INT,
    email VARCHAR(255),
    display_name VARCHAR(200),
    success BOOLEAN,
    message VARCHAR(255)
) AS $$
DECLARE
    v_user_id INT;
    v_display_name VARCHAR(200);
BEGIN
    -- Check if email already exists
    IF EXISTS (SELECT 1 FROM Users WHERE Email = p_email) THEN
        RETURN QUERY SELECT NULL::INT, p_email, NULL::VARCHAR(200), FALSE, 'Email already exists'::VARCHAR(255);
        RETURN;
    END IF;

    v_display_name := p_first_name || ' ' || p_last_name;

    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, CreatedBy)
    VALUES (p_email, p_password_hash, p_password_salt, p_first_name, p_last_name, v_display_name, p_phone, p_created_by)
    RETURNING Id INTO v_user_id;

    -- Log the action
    INSERT INTO AuditLogs (UserId, Action, EntityType, EntityId, NewValues)
    VALUES (p_created_by, 'CREATE', 'User', v_user_id, jsonb_build_object('email', p_email, 'name', v_display_name));

    RETURN QUERY SELECT v_user_id, p_email, v_display_name, TRUE, 'User created successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- GET USER BY ID
-- =====================================================
CREATE OR REPLACE FUNCTION sp_user_get_by_id(p_user_id INT)
RETURNS TABLE(
    id INT,
    email VARCHAR(255),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    display_name VARCHAR(200),
    phone VARCHAR(20),
    avatar_url VARCHAR(500),
    is_active BOOLEAN,
    is_locked BOOLEAN,
    last_login_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT u.Id, u.Email, u.FirstName, u.LastName, u.DisplayName, u.Phone,
           u.AvatarUrl, u.IsActive, u.IsLocked, u.LastLoginAt, u.CreatedAt
    FROM Users u
    WHERE u.Id = p_user_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- GET USER BY EMAIL (for login)
-- =====================================================
CREATE OR REPLACE FUNCTION sp_user_get_by_email(p_email VARCHAR(255))
RETURNS TABLE(
    id INT,
    email VARCHAR(255),
    password_hash VARCHAR(255),
    password_salt VARCHAR(255),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    display_name VARCHAR(200),
    is_active BOOLEAN,
    is_locked BOOLEAN,
    failed_login_attempts INT,
    must_change_password BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    SELECT u.Id, u.Email, u.PasswordHash, u.PasswordSalt, u.FirstName, u.LastName,
           u.DisplayName, u.IsActive, u.IsLocked, u.FailedLoginAttempts, u.MustChangePassword
    FROM Users u
    WHERE u.Email = p_email;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- UPDATE USER
-- =====================================================
CREATE OR REPLACE FUNCTION sp_user_update(
    p_user_id INT,
    p_first_name VARCHAR(100),
    p_last_name VARCHAR(100),
    p_phone VARCHAR(20) DEFAULT NULL,
    p_avatar_url VARCHAR(500) DEFAULT NULL,
    p_updated_by INT DEFAULT NULL
)
RETURNS TABLE(success BOOLEAN, message VARCHAR(255)) AS $$
DECLARE
    v_old_values JSONB;
BEGIN
    -- Get old values for audit
    SELECT jsonb_build_object(
        'firstName', FirstName,
        'lastName', LastName,
        'phone', Phone,
        'avatarUrl', AvatarUrl
    ) INTO v_old_values
    FROM Users WHERE Id = p_user_id;

    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'User not found'::VARCHAR(255);
        RETURN;
    END IF;

    UPDATE Users
    SET FirstName = p_first_name,
        LastName = p_last_name,
        DisplayName = p_first_name || ' ' || p_last_name,
        Phone = COALESCE(p_phone, Phone),
        AvatarUrl = COALESCE(p_avatar_url, AvatarUrl),
        UpdatedAt = CURRENT_TIMESTAMP,
        UpdatedBy = p_updated_by
    WHERE Id = p_user_id;

    -- Log the action
    INSERT INTO AuditLogs (UserId, Action, EntityType, EntityId, OldValues, NewValues)
    VALUES (p_updated_by, 'UPDATE', 'User', p_user_id, v_old_values,
            jsonb_build_object('firstName', p_first_name, 'lastName', p_last_name, 'phone', p_phone));

    RETURN QUERY SELECT TRUE, 'User updated successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- DELETE USER (Soft Delete)
-- =====================================================
CREATE OR REPLACE FUNCTION sp_user_delete(
    p_user_id INT,
    p_deleted_by INT
)
RETURNS TABLE(success BOOLEAN, message VARCHAR(255)) AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = p_user_id) THEN
        RETURN QUERY SELECT FALSE, 'User not found'::VARCHAR(255);
        RETURN;
    END IF;

    UPDATE Users
    SET IsActive = FALSE,
        UpdatedAt = CURRENT_TIMESTAMP,
        UpdatedBy = p_deleted_by
    WHERE Id = p_user_id;

    -- Log the action
    INSERT INTO AuditLogs (UserId, Action, EntityType, EntityId)
    VALUES (p_deleted_by, 'DELETE', 'User', p_user_id);

    RETURN QUERY SELECT TRUE, 'User deactivated successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- LIST USERS WITH PAGINATION
-- =====================================================
CREATE OR REPLACE FUNCTION sp_user_list(
    p_page_number INT DEFAULT 1,
    p_page_size INT DEFAULT 20,
    p_search VARCHAR(100) DEFAULT NULL,
    p_is_active BOOLEAN DEFAULT NULL,
    p_role_id INT DEFAULT NULL
)
RETURNS TABLE(
    id INT,
    email VARCHAR(255),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    display_name VARCHAR(200),
    is_active BOOLEAN,
    is_locked BOOLEAN,
    created_at TIMESTAMP WITH TIME ZONE,
    roles TEXT,
    total_count BIGINT
) AS $$
DECLARE
    v_offset INT;
    v_total BIGINT;
BEGIN
    v_offset := (p_page_number - 1) * p_page_size;

    -- Get total count
    SELECT COUNT(*) INTO v_total
    FROM Users u
    LEFT JOIN UserRoles ur ON u.Id = ur.UserId
    WHERE (p_search IS NULL OR u.Email ILIKE '%' || p_search || '%' OR u.DisplayName ILIKE '%' || p_search || '%')
      AND (p_is_active IS NULL OR u.IsActive = p_is_active)
      AND (p_role_id IS NULL OR ur.RoleId = p_role_id);

    RETURN QUERY
    SELECT u.Id, u.Email, u.FirstName, u.LastName, u.DisplayName, u.IsActive, u.IsLocked, u.CreatedAt,
           STRING_AGG(r.Name, ', ') AS roles,
           v_total AS total_count
    FROM Users u
    LEFT JOIN UserRoles ur ON u.Id = ur.UserId
    LEFT JOIN Roles r ON ur.RoleId = r.Id
    WHERE (p_search IS NULL OR u.Email ILIKE '%' || p_search || '%' OR u.DisplayName ILIKE '%' || p_search || '%')
      AND (p_is_active IS NULL OR u.IsActive = p_is_active)
      AND (p_role_id IS NULL OR ur.RoleId = p_role_id)
    GROUP BY u.Id, u.Email, u.FirstName, u.LastName, u.DisplayName, u.IsActive, u.IsLocked, u.CreatedAt
    ORDER BY u.CreatedAt DESC
    LIMIT p_page_size OFFSET v_offset;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- ASSIGN ROLE TO USER
-- =====================================================
CREATE OR REPLACE FUNCTION sp_user_assign_role(
    p_user_id INT,
    p_role_id INT,
    p_assigned_by INT
)
RETURNS TABLE(success BOOLEAN, message VARCHAR(255)) AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = p_user_id) THEN
        RETURN QUERY SELECT FALSE, 'User not found'::VARCHAR(255);
        RETURN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = p_role_id) THEN
        RETURN QUERY SELECT FALSE, 'Role not found'::VARCHAR(255);
        RETURN;
    END IF;

    INSERT INTO UserRoles (UserId, RoleId, AssignedBy)
    VALUES (p_user_id, p_role_id, p_assigned_by)
    ON CONFLICT (UserId, RoleId) DO NOTHING;

    -- Log the action
    INSERT INTO AuditLogs (UserId, Action, EntityType, EntityId, NewValues)
    VALUES (p_assigned_by, 'ASSIGN_ROLE', 'UserRole', p_user_id,
            jsonb_build_object('roleId', p_role_id));

    RETURN QUERY SELECT TRUE, 'Role assigned successfully'::VARCHAR(255);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- GET USER ROLES
-- =====================================================
CREATE OR REPLACE FUNCTION sp_user_get_roles(p_user_id INT)
RETURNS TABLE(
    role_id INT,
    role_name VARCHAR(50),
    permissions JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT r.Id, r.Name, r.Permissions
    FROM UserRoles ur
    JOIN Roles r ON ur.RoleId = r.Id
    WHERE ur.UserId = p_user_id AND r.IsActive = TRUE;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- UPDATE LOGIN ATTEMPT (for security)
-- =====================================================
CREATE OR REPLACE FUNCTION sp_user_update_login_attempt(
    p_user_id INT,
    p_success BOOLEAN,
    p_ip_address VARCHAR(45) DEFAULT NULL,
    p_user_agent VARCHAR(500) DEFAULT NULL
)
RETURNS VOID AS $$
BEGIN
    IF p_success THEN
        UPDATE Users
        SET LastLoginAt = CURRENT_TIMESTAMP,
            FailedLoginAttempts = 0,
            IsLocked = FALSE
        WHERE Id = p_user_id;

        INSERT INTO AuditLogs (UserId, Action, IpAddress, UserAgent)
        VALUES (p_user_id, 'LOGIN_SUCCESS', p_ip_address, p_user_agent);
    ELSE
        UPDATE Users
        SET FailedLoginAttempts = FailedLoginAttempts + 1,
            IsLocked = CASE WHEN FailedLoginAttempts >= 4 THEN TRUE ELSE FALSE END
        WHERE Id = p_user_id;

        INSERT INTO AuditLogs (UserId, Action, IpAddress, UserAgent)
        VALUES (p_user_id, 'LOGIN_FAILED', p_ip_address, p_user_agent);
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
