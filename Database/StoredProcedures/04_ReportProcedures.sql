-- =====================================================
-- REPORTING STORED PROCEDURES
-- =====================================================

-- =====================================================
-- DASHBOARD SUMMARY REPORT
-- =====================================================
CREATE OR REPLACE FUNCTION sp_report_dashboard_summary(
    p_user_id INT DEFAULT NULL,
    p_user_role VARCHAR(50) DEFAULT 'User',
    p_team_id INT DEFAULT NULL
)
RETURNS TABLE(
    total_tickets BIGINT,
    open_tickets BIGINT,
    pending_tickets BIGINT,
    resolved_today BIGINT,
    overdue_tickets BIGINT,
    unassigned_tickets BIGINT,
    avg_resolution_hours NUMERIC,
    sla_breached_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(*) AS total_tickets,
        COUNT(*) FILTER (WHERE s.IsClosed = FALSE) AS open_tickets,
        COUNT(*) FILTER (WHERE s.Name = 'Pending') AS pending_tickets,
        COUNT(*) FILTER (WHERE t.ResolvedAt::DATE = CURRENT_DATE) AS resolved_today,
        COUNT(*) FILTER (WHERE t.DueDate < CURRENT_TIMESTAMP AND s.IsClosed = FALSE) AS overdue_tickets,
        COUNT(*) FILTER (WHERE t.AssignedToId IS NULL AND s.IsClosed = FALSE) AS unassigned_tickets,
        ROUND(AVG(EXTRACT(EPOCH FROM (t.ResolvedAt - t.CreatedAt)) / 3600) 
              FILTER (WHERE t.ResolvedAt IS NOT NULL), 2) AS avg_resolution_hours,
        COUNT(*) FILTER (WHERE t.SlaBreached = TRUE) AS sla_breached_count
    FROM Tickets t
    JOIN Statuses s ON t.StatusId = s.Id
    WHERE t.IsDeleted = FALSE
      AND (
          p_user_role IN ('Administrator', 'Manager') 
          OR (p_user_role = 'Agent' AND (t.AssignedToId = p_user_id OR t.AssignedTeamId = p_team_id))
          OR (p_user_role = 'User' AND t.RequesterId = p_user_id)
      );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- TICKETS BY STATUS REPORT
-- =====================================================
CREATE OR REPLACE FUNCTION sp_report_tickets_by_status(
    p_date_from TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_date_to TIMESTAMP WITH TIME ZONE DEFAULT NULL
)
RETURNS TABLE(
    status_id INT,
    status_name VARCHAR(50),
    status_color VARCHAR(7),
    ticket_count BIGINT,
    percentage NUMERIC
) AS $$
DECLARE
    v_total BIGINT;
BEGIN
    SELECT COUNT(*) INTO v_total
    FROM Tickets t
    WHERE t.IsDeleted = FALSE
      AND (p_date_from IS NULL OR t.CreatedAt >= p_date_from)
      AND (p_date_to IS NULL OR t.CreatedAt <= p_date_to);

    RETURN QUERY
    SELECT s.Id, s.Name, s.Color,
           COUNT(t.Id) AS ticket_count,
           ROUND(COUNT(t.Id)::NUMERIC * 100 / NULLIF(v_total, 0), 2) AS percentage
    FROM Statuses s
    LEFT JOIN Tickets t ON t.StatusId = s.Id 
        AND t.IsDeleted = FALSE
        AND (p_date_from IS NULL OR t.CreatedAt >= p_date_from)
        AND (p_date_to IS NULL OR t.CreatedAt <= p_date_to)
    WHERE s.IsActive = TRUE
    GROUP BY s.Id, s.Name, s.Color, s.DisplayOrder
    ORDER BY s.DisplayOrder;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- TICKETS BY PRIORITY REPORT
-- =====================================================
CREATE OR REPLACE FUNCTION sp_report_tickets_by_priority(
    p_date_from TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_date_to TIMESTAMP WITH TIME ZONE DEFAULT NULL
)
RETURNS TABLE(
    priority_id INT,
    priority_name VARCHAR(50),
    priority_color VARCHAR(7),
    ticket_count BIGINT,
    open_count BIGINT,
    avg_resolution_hours NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT p.Id, p.Name, p.Color,
           COUNT(t.Id) AS ticket_count,
           COUNT(t.Id) FILTER (WHERE st.IsClosed = FALSE) AS open_count,
           ROUND(AVG(EXTRACT(EPOCH FROM (t.ResolvedAt - t.CreatedAt)) / 3600) 
                 FILTER (WHERE t.ResolvedAt IS NOT NULL), 2) AS avg_resolution_hours
    FROM Priorities p
    LEFT JOIN Tickets t ON t.PriorityId = p.Id 
        AND t.IsDeleted = FALSE
        AND (p_date_from IS NULL OR t.CreatedAt >= p_date_from)
        AND (p_date_to IS NULL OR t.CreatedAt <= p_date_to)
    LEFT JOIN Statuses st ON t.StatusId = st.Id
    WHERE p.IsActive = TRUE
    GROUP BY p.Id, p.Name, p.Color, p.Level
    ORDER BY p.Level;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- TICKETS BY CATEGORY REPORT
-- =====================================================
CREATE OR REPLACE FUNCTION sp_report_tickets_by_category(
    p_date_from TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_date_to TIMESTAMP WITH TIME ZONE DEFAULT NULL
)
RETURNS TABLE(
    category_id INT,
    category_name VARCHAR(100),
    parent_category VARCHAR(100),
    ticket_count BIGINT,
    open_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT c.Id, c.Name, pc.Name AS parent_category,
           COUNT(t.Id) AS ticket_count,
           COUNT(t.Id) FILTER (WHERE st.IsClosed = FALSE) AS open_count
    FROM Categories c
    LEFT JOIN Categories pc ON c.ParentId = pc.Id
    LEFT JOIN Tickets t ON t.CategoryId = c.Id 
        AND t.IsDeleted = FALSE
        AND (p_date_from IS NULL OR t.CreatedAt >= p_date_from)
        AND (p_date_to IS NULL OR t.CreatedAt <= p_date_to)
    LEFT JOIN Statuses st ON t.StatusId = st.Id
    WHERE c.IsActive = TRUE
    GROUP BY c.Id, c.Name, pc.Name
    ORDER BY c.Name;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- TEAM PERFORMANCE REPORT
-- =====================================================
CREATE OR REPLACE FUNCTION sp_report_team_performance(
    p_date_from TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_date_to TIMESTAMP WITH TIME ZONE DEFAULT NULL
)
RETURNS TABLE(
    team_id INT,
    team_name VARCHAR(100),
    total_assigned BIGINT,
    resolved_count BIGINT,
    open_count BIGINT,
    avg_resolution_hours NUMERIC,
    avg_first_response_hours NUMERIC,
    sla_compliance_percent NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT tm.Id, tm.Name,
           COUNT(t.Id) AS total_assigned,
           COUNT(t.Id) FILTER (WHERE st.Name = 'Resolved' OR st.IsClosed = TRUE) AS resolved_count,
           COUNT(t.Id) FILTER (WHERE st.IsClosed = FALSE) AS open_count,
           ROUND(AVG(EXTRACT(EPOCH FROM (t.ResolvedAt - t.CreatedAt)) / 3600) 
                 FILTER (WHERE t.ResolvedAt IS NOT NULL), 2) AS avg_resolution_hours,
           ROUND(AVG(EXTRACT(EPOCH FROM (t.FirstResponseAt - t.CreatedAt)) / 3600) 
                 FILTER (WHERE t.FirstResponseAt IS NOT NULL), 2) AS avg_first_response_hours,
           ROUND(
               COUNT(*) FILTER (WHERE t.SlaBreached = FALSE)::NUMERIC * 100 / 
               NULLIF(COUNT(*), 0), 2
           ) AS sla_compliance_percent
    FROM Teams tm
    LEFT JOIN Tickets t ON t.AssignedTeamId = tm.Id 
        AND t.IsDeleted = FALSE
        AND (p_date_from IS NULL OR t.CreatedAt >= p_date_from)
        AND (p_date_to IS NULL OR t.CreatedAt <= p_date_to)
    LEFT JOIN Statuses st ON t.StatusId = st.Id
    WHERE tm.IsActive = TRUE
    GROUP BY tm.Id, tm.Name
    ORDER BY tm.Name;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- AGENT PERFORMANCE REPORT
-- =====================================================
CREATE OR REPLACE FUNCTION sp_report_agent_performance(
    p_team_id INT DEFAULT NULL,
    p_date_from TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_date_to TIMESTAMP WITH TIME ZONE DEFAULT NULL
)
RETURNS TABLE(
    user_id INT,
    user_name VARCHAR(200),
    team_name VARCHAR(100),
    total_assigned BIGINT,
    resolved_count BIGINT,
    open_count BIGINT,
    avg_resolution_hours NUMERIC,
    avg_first_response_hours NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT u.Id, u.DisplayName, tm.Name AS team_name,
           COUNT(t.Id) AS total_assigned,
           COUNT(t.Id) FILTER (WHERE st.Name = 'Resolved' OR st.IsClosed = TRUE) AS resolved_count,
           COUNT(t.Id) FILTER (WHERE st.IsClosed = FALSE) AS open_count,
           ROUND(AVG(EXTRACT(EPOCH FROM (t.ResolvedAt - t.CreatedAt)) / 3600) 
                 FILTER (WHERE t.ResolvedAt IS NOT NULL), 2) AS avg_resolution_hours,
           ROUND(AVG(EXTRACT(EPOCH FROM (t.FirstResponseAt - t.CreatedAt)) / 3600) 
                 FILTER (WHERE t.FirstResponseAt IS NOT NULL), 2) AS avg_first_response_hours
    FROM Users u
    JOIN UserRoles ur ON u.Id = ur.UserId
    JOIN Roles r ON ur.RoleId = r.Id AND r.Name IN ('Agent', 'Manager')
    LEFT JOIN TeamMembers tmem ON u.Id = tmem.UserId
    LEFT JOIN Teams tm ON tmem.TeamId = tm.Id
    LEFT JOIN Tickets t ON t.AssignedToId = u.Id 
        AND t.IsDeleted = FALSE
        AND (p_date_from IS NULL OR t.CreatedAt >= p_date_from)
        AND (p_date_to IS NULL OR t.CreatedAt <= p_date_to)
    LEFT JOIN Statuses st ON t.StatusId = st.Id
    WHERE u.IsActive = TRUE
      AND (p_team_id IS NULL OR tm.Id = p_team_id)
    GROUP BY u.Id, u.DisplayName, tm.Name
    ORDER BY u.DisplayName;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- TICKET TREND REPORT (Daily/Weekly/Monthly)
-- =====================================================
CREATE OR REPLACE FUNCTION sp_report_ticket_trend(
    p_period VARCHAR(10) DEFAULT 'daily', -- daily, weekly, monthly
    p_date_from TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_DATE - INTERVAL '30 days',
    p_date_to TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
)
RETURNS TABLE(
    period_date DATE,
    created_count BIGINT,
    resolved_count BIGINT,
    closed_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    WITH date_series AS (
        SELECT generate_series(
            p_date_from::DATE,
            p_date_to::DATE,
            CASE p_period
                WHEN 'daily' THEN '1 day'::INTERVAL
                WHEN 'weekly' THEN '1 week'::INTERVAL
                WHEN 'monthly' THEN '1 month'::INTERVAL
            END
        )::DATE AS period_date
    )
    SELECT ds.period_date,
           COUNT(t.Id) FILTER (WHERE t.CreatedAt::DATE = ds.period_date) AS created_count,
           COUNT(t.Id) FILTER (WHERE t.ResolvedAt::DATE = ds.period_date) AS resolved_count,
           COUNT(t.Id) FILTER (WHERE t.ClosedAt::DATE = ds.period_date) AS closed_count
    FROM date_series ds
    LEFT JOIN Tickets t ON (
        (p_period = 'daily' AND t.CreatedAt::DATE = ds.period_date)
        OR (p_period = 'weekly' AND DATE_TRUNC('week', t.CreatedAt) = DATE_TRUNC('week', ds.period_date::TIMESTAMP))
        OR (p_period = 'monthly' AND DATE_TRUNC('month', t.CreatedAt) = DATE_TRUNC('month', ds.period_date::TIMESTAMP))
    )
    AND t.IsDeleted = FALSE
    GROUP BY ds.period_date
    ORDER BY ds.period_date;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- SLA COMPLIANCE REPORT
-- =====================================================
CREATE OR REPLACE FUNCTION sp_report_sla_compliance(
    p_date_from TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_date_to TIMESTAMP WITH TIME ZONE DEFAULT NULL
)
RETURNS TABLE(
    priority_name VARCHAR(50),
    total_tickets BIGINT,
    within_sla_response BIGINT,
    breached_sla_response BIGINT,
    within_sla_resolution BIGINT,
    breached_sla_resolution BIGINT,
    response_compliance_percent NUMERIC,
    resolution_compliance_percent NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT p.Name AS priority_name,
           COUNT(t.Id) AS total_tickets,
           COUNT(t.Id) FILTER (
               WHERE t.FirstResponseAt IS NOT NULL 
               AND EXTRACT(EPOCH FROM (t.FirstResponseAt - t.CreatedAt)) / 3600 <= p.SlaResponseHours
           ) AS within_sla_response,
           COUNT(t.Id) FILTER (
               WHERE t.FirstResponseAt IS NOT NULL 
               AND EXTRACT(EPOCH FROM (t.FirstResponseAt - t.CreatedAt)) / 3600 > p.SlaResponseHours
           ) AS breached_sla_response,
           COUNT(t.Id) FILTER (
               WHERE t.ResolvedAt IS NOT NULL 
               AND EXTRACT(EPOCH FROM (t.ResolvedAt - t.CreatedAt)) / 3600 <= p.SlaResolutionHours
           ) AS within_sla_resolution,
           COUNT(t.Id) FILTER (
               WHERE t.ResolvedAt IS NOT NULL 
               AND EXTRACT(EPOCH FROM (t.ResolvedAt - t.CreatedAt)) / 3600 > p.SlaResolutionHours
           ) AS breached_sla_resolution,
           ROUND(
               COUNT(t.Id) FILTER (
                   WHERE t.FirstResponseAt IS NOT NULL 
                   AND EXTRACT(EPOCH FROM (t.FirstResponseAt - t.CreatedAt)) / 3600 <= p.SlaResponseHours
               )::NUMERIC * 100 / NULLIF(COUNT(t.Id) FILTER (WHERE t.FirstResponseAt IS NOT NULL), 0), 2
           ) AS response_compliance_percent,
           ROUND(
               COUNT(t.Id) FILTER (
                   WHERE t.ResolvedAt IS NOT NULL 
                   AND EXTRACT(EPOCH FROM (t.ResolvedAt - t.CreatedAt)) / 3600 <= p.SlaResolutionHours
               )::NUMERIC * 100 / NULLIF(COUNT(t.Id) FILTER (WHERE t.ResolvedAt IS NOT NULL), 0), 2
           ) AS resolution_compliance_percent
    FROM Priorities p
    LEFT JOIN Tickets t ON t.PriorityId = p.Id 
        AND t.IsDeleted = FALSE
        AND (p_date_from IS NULL OR t.CreatedAt >= p_date_from)
        AND (p_date_to IS NULL OR t.CreatedAt <= p_date_to)
    WHERE p.IsActive = TRUE
    GROUP BY p.Id, p.Name, p.Level
    ORDER BY p.Level;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- LOOKUP DATA STORED PROCEDURES
-- =====================================================

-- Get all priorities
CREATE OR REPLACE FUNCTION sp_lookup_priorities()
RETURNS TABLE(id INT, name VARCHAR(50), color VARCHAR(7), level INT) AS $$
BEGIN
    RETURN QUERY SELECT p.Id, p.Name, p.Color, p.Level FROM Priorities p WHERE p.IsActive = TRUE ORDER BY p.Level;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Get all statuses
CREATE OR REPLACE FUNCTION sp_lookup_statuses()
RETURNS TABLE(id INT, name VARCHAR(50), color VARCHAR(7), is_closed BOOLEAN) AS $$
BEGIN
    RETURN QUERY SELECT s.Id, s.Name, s.Color, s.IsClosed FROM Statuses s WHERE s.IsActive = TRUE ORDER BY s.DisplayOrder;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Get all categories (hierarchical)
CREATE OR REPLACE FUNCTION sp_lookup_categories()
RETURNS TABLE(id INT, name VARCHAR(100), parent_id INT, parent_name VARCHAR(100)) AS $$
BEGIN
    RETURN QUERY 
    SELECT c.Id, c.Name, c.ParentId, pc.Name AS parent_name
    FROM Categories c
    LEFT JOIN Categories pc ON c.ParentId = pc.Id
    WHERE c.IsActive = TRUE
    ORDER BY COALESCE(pc.Name, c.Name), c.Name;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Get all active teams
CREATE OR REPLACE FUNCTION sp_lookup_teams()
RETURNS TABLE(id INT, name VARCHAR(100)) AS $$
BEGIN
    RETURN QUERY SELECT t.Id, t.Name FROM Teams t WHERE t.IsActive = TRUE ORDER BY t.Name;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Get all agents (users with Agent or Manager role)
CREATE OR REPLACE FUNCTION sp_lookup_agents()
RETURNS TABLE(id INT, display_name VARCHAR(200), email VARCHAR(255)) AS $$
BEGIN
    RETURN QUERY 
    SELECT DISTINCT u.Id, u.DisplayName, u.Email
    FROM Users u
    JOIN UserRoles ur ON u.Id = ur.UserId
    JOIN Roles r ON ur.RoleId = r.Id
    WHERE u.IsActive = TRUE AND r.Name IN ('Agent', 'Manager', 'Administrator')
    ORDER BY u.DisplayName;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
