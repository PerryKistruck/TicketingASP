-- =====================================================
-- TEST DATA FOR TICKETING SYSTEM
-- Description: Creates test users, team assignments, and tickets
-- Note: All test users have password "Test@123"
-- The hash/salt below are pre-computed for this password
-- =====================================================

-- Pre-computed hash and salt for password - using existing user's credentials
-- All test users will have the same password as the admin account
DO $$
DECLARE
    v_test_hash VARCHAR(255) := 'Epuwo9diXAACngEJUbp+KlOGRUYJr76MtCmix4nYIv9Fb5HdH5FdIudOv7t3dyyKG1ZaZ8aqOsL9HNebbgt0xQ==';
    v_test_salt VARCHAR(255) := '8Pdi6g8w2skQBXA4D1edjs7PQJj7FyH3q7ID7h2Ze7CYDUdTukN5MRsjFWWsu2PFseElpjuuXvemP+5dBU4rMDOCx2V10fS+gYuBQLKIFF/0XSjWHbClENk6uZ9M0SO6NXoCPeCWP4duyuy9mtPN3fUb51WBuodu6kk1/TFYC38=';
    
    -- User IDs (will be populated)
    v_admin_id INT;
    v_agent1_id INT;
    v_agent2_id INT;
    v_agent3_id INT;
    v_agent4_id INT;
    v_agent5_id INT;
    v_agent6_id INT;
    v_agent7_id INT;
    v_agent8_id INT;
    v_manager1_id INT;
    v_manager2_id INT;
    v_manager3_id INT;
    v_user1_id INT;
    v_user2_id INT;
    v_user3_id INT;
    v_user4_id INT;
    v_user5_id INT;
    v_user6_id INT;
    v_user7_id INT;
    v_user8_id INT;
    
    -- Team IDs
    v_it_support_id INT;
    v_network_team_id INT;
    v_app_support_id INT;
    v_helpdesk_id INT;
    
    -- Role IDs
    v_admin_role_id INT;
    v_manager_role_id INT;
    v_agent_role_id INT;
    v_user_role_id INT;
    
    -- Status IDs
    v_status_new INT;
    v_status_open INT;
    v_status_in_progress INT;
    v_status_pending INT;
    v_status_on_hold INT;
    v_status_resolved INT;
    v_status_closed INT;
    
    -- Priority IDs
    v_priority_critical INT;
    v_priority_high INT;
    v_priority_medium INT;
    v_priority_low INT;
    
    -- Category IDs
    v_cat_hardware INT;
    v_cat_software INT;
    v_cat_network INT;
    v_cat_account INT;
    v_cat_other INT;
    
BEGIN
    -- Get Role IDs
    SELECT Id INTO v_admin_role_id FROM Roles WHERE Name = 'Administrator';
    SELECT Id INTO v_manager_role_id FROM Roles WHERE Name = 'Manager';
    SELECT Id INTO v_agent_role_id FROM Roles WHERE Name = 'Agent';
    SELECT Id INTO v_user_role_id FROM Roles WHERE Name = 'User';
    
    -- Get Team IDs
    SELECT Id INTO v_it_support_id FROM Teams WHERE Name = 'IT Support';
    SELECT Id INTO v_network_team_id FROM Teams WHERE Name = 'Network Team';
    SELECT Id INTO v_app_support_id FROM Teams WHERE Name = 'Application Support';
    SELECT Id INTO v_helpdesk_id FROM Teams WHERE Name = 'Help Desk';
    
    -- Get Status IDs
    SELECT Id INTO v_status_new FROM Statuses WHERE Name = 'New';
    SELECT Id INTO v_status_open FROM Statuses WHERE Name = 'Open';
    SELECT Id INTO v_status_in_progress FROM Statuses WHERE Name = 'In Progress';
    SELECT Id INTO v_status_pending FROM Statuses WHERE Name = 'Pending';
    SELECT Id INTO v_status_on_hold FROM Statuses WHERE Name = 'On Hold';
    SELECT Id INTO v_status_resolved FROM Statuses WHERE Name = 'Resolved';
    SELECT Id INTO v_status_closed FROM Statuses WHERE Name = 'Closed';
    
    -- Get Priority IDs
    SELECT Id INTO v_priority_critical FROM Priorities WHERE Level = 1;
    SELECT Id INTO v_priority_high FROM Priorities WHERE Level = 2;
    SELECT Id INTO v_priority_medium FROM Priorities WHERE Level = 3;
    SELECT Id INTO v_priority_low FROM Priorities WHERE Level = 4;
    
    -- Get Category IDs
    SELECT Id INTO v_cat_hardware FROM Categories WHERE Name = 'Hardware' AND ParentId IS NULL;
    SELECT Id INTO v_cat_software FROM Categories WHERE Name = 'Software' AND ParentId IS NULL;
    SELECT Id INTO v_cat_network FROM Categories WHERE Name = 'Network' AND ParentId IS NULL;
    SELECT Id INTO v_cat_account FROM Categories WHERE Name = 'Account' AND ParentId IS NULL;
    SELECT Id INTO v_cat_other FROM Categories WHERE Name = 'Other' AND ParentId IS NULL;

    -- =====================================================
    -- CREATE TEAM MANAGERS (3 managers for IT Support, Network Team, Help Desk)
    -- Note: Application Support already has a manager (you)
    -- =====================================================
    
    -- Manager for IT Support
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('sarah.mitchell@company.com', v_test_hash, v_test_salt, 'Sarah', 'Mitchell', 'Sarah Mitchell', '0412-345-678', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_manager1_id;
    IF v_manager1_id IS NULL THEN SELECT Id INTO v_manager1_id FROM Users WHERE Email = 'sarah.mitchell@company.com'; END IF;
    
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_manager1_id, v_manager_role_id) ON CONFLICT DO NOTHING;
    UPDATE Teams SET ManagerId = v_manager1_id WHERE Id = v_it_support_id;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_it_support_id, v_manager1_id, 'Leader') ON CONFLICT DO NOTHING;
    
    -- Manager for Network Team
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('david.chen@company.com', v_test_hash, v_test_salt, 'David', 'Chen', 'David Chen', '0413-456-789', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_manager2_id;
    IF v_manager2_id IS NULL THEN SELECT Id INTO v_manager2_id FROM Users WHERE Email = 'david.chen@company.com'; END IF;
    
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_manager2_id, v_manager_role_id) ON CONFLICT DO NOTHING;
    UPDATE Teams SET ManagerId = v_manager2_id WHERE Id = v_network_team_id;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_network_team_id, v_manager2_id, 'Leader') ON CONFLICT DO NOTHING;
    
    -- Manager for Help Desk
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('emma.wilson@company.com', v_test_hash, v_test_salt, 'Emma', 'Wilson', 'Emma Wilson', '0414-567-890', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_manager3_id;
    IF v_manager3_id IS NULL THEN SELECT Id INTO v_manager3_id FROM Users WHERE Email = 'emma.wilson@company.com'; END IF;
    
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_manager3_id, v_manager_role_id) ON CONFLICT DO NOTHING;
    UPDATE Teams SET ManagerId = v_manager3_id WHERE Id = v_helpdesk_id;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_helpdesk_id, v_manager3_id, 'Leader') ON CONFLICT DO NOTHING;

    -- Set Perry Kistruck (user id 1) as manager of Application Support team
    UPDATE Teams SET ManagerId = 1 WHERE Id = v_app_support_id;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_app_support_id, 1, 'Leader') ON CONFLICT DO NOTHING;

    -- =====================================================
    -- CREATE AGENTS (8 agents across teams)
    -- =====================================================
    
    -- IT Support Agents
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('james.taylor@company.com', v_test_hash, v_test_salt, 'James', 'Taylor', 'James Taylor', '0420-111-222', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_agent1_id;
    IF v_agent1_id IS NULL THEN SELECT Id INTO v_agent1_id FROM Users WHERE Email = 'james.taylor@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_agent1_id, v_agent_role_id) ON CONFLICT DO NOTHING;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_it_support_id, v_agent1_id, 'Member') ON CONFLICT DO NOTHING;
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('olivia.brown@company.com', v_test_hash, v_test_salt, 'Olivia', 'Brown', 'Olivia Brown', '0420-222-333', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_agent2_id;
    IF v_agent2_id IS NULL THEN SELECT Id INTO v_agent2_id FROM Users WHERE Email = 'olivia.brown@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_agent2_id, v_agent_role_id) ON CONFLICT DO NOTHING;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_it_support_id, v_agent2_id, 'Member') ON CONFLICT DO NOTHING;
    
    -- Network Team Agents
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('michael.lee@company.com', v_test_hash, v_test_salt, 'Michael', 'Lee', 'Michael Lee', '0420-333-444', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_agent3_id;
    IF v_agent3_id IS NULL THEN SELECT Id INTO v_agent3_id FROM Users WHERE Email = 'michael.lee@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_agent3_id, v_agent_role_id) ON CONFLICT DO NOTHING;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_network_team_id, v_agent3_id, 'Member') ON CONFLICT DO NOTHING;
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('sophia.garcia@company.com', v_test_hash, v_test_salt, 'Sophia', 'Garcia', 'Sophia Garcia', '0420-444-555', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_agent4_id;
    IF v_agent4_id IS NULL THEN SELECT Id INTO v_agent4_id FROM Users WHERE Email = 'sophia.garcia@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_agent4_id, v_agent_role_id) ON CONFLICT DO NOTHING;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_network_team_id, v_agent4_id, 'Member') ON CONFLICT DO NOTHING;
    
    -- Application Support Agents
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('william.jones@company.com', v_test_hash, v_test_salt, 'William', 'Jones', 'William Jones', '0420-555-666', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_agent5_id;
    IF v_agent5_id IS NULL THEN SELECT Id INTO v_agent5_id FROM Users WHERE Email = 'william.jones@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_agent5_id, v_agent_role_id) ON CONFLICT DO NOTHING;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_app_support_id, v_agent5_id, 'Member') ON CONFLICT DO NOTHING;
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('ava.martinez@company.com', v_test_hash, v_test_salt, 'Ava', 'Martinez', 'Ava Martinez', '0420-666-777', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_agent6_id;
    IF v_agent6_id IS NULL THEN SELECT Id INTO v_agent6_id FROM Users WHERE Email = 'ava.martinez@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_agent6_id, v_agent_role_id) ON CONFLICT DO NOTHING;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_app_support_id, v_agent6_id, 'Member') ON CONFLICT DO NOTHING;
    
    -- Help Desk Agents
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('ethan.davis@company.com', v_test_hash, v_test_salt, 'Ethan', 'Davis', 'Ethan Davis', '0420-777-888', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_agent7_id;
    IF v_agent7_id IS NULL THEN SELECT Id INTO v_agent7_id FROM Users WHERE Email = 'ethan.davis@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_agent7_id, v_agent_role_id) ON CONFLICT DO NOTHING;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_helpdesk_id, v_agent7_id, 'Member') ON CONFLICT DO NOTHING;
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('isabella.rodriguez@company.com', v_test_hash, v_test_salt, 'Isabella', 'Rodriguez', 'Isabella Rodriguez', '0420-888-999', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_agent8_id;
    IF v_agent8_id IS NULL THEN SELECT Id INTO v_agent8_id FROM Users WHERE Email = 'isabella.rodriguez@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_agent8_id, v_agent_role_id) ON CONFLICT DO NOTHING;
    INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (v_helpdesk_id, v_agent8_id, 'Member') ON CONFLICT DO NOTHING;

    -- =====================================================
    -- CREATE REGULAR USERS (8 users who will create tickets)
    -- =====================================================
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('john.smith@company.com', v_test_hash, v_test_salt, 'John', 'Smith', 'John Smith', '0430-111-111', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_user1_id;
    IF v_user1_id IS NULL THEN SELECT Id INTO v_user1_id FROM Users WHERE Email = 'john.smith@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_user1_id, v_user_role_id) ON CONFLICT DO NOTHING;
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('emily.johnson@company.com', v_test_hash, v_test_salt, 'Emily', 'Johnson', 'Emily Johnson', '0430-222-222', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_user2_id;
    IF v_user2_id IS NULL THEN SELECT Id INTO v_user2_id FROM Users WHERE Email = 'emily.johnson@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_user2_id, v_user_role_id) ON CONFLICT DO NOTHING;
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('robert.williams@company.com', v_test_hash, v_test_salt, 'Robert', 'Williams', 'Robert Williams', '0430-333-333', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_user3_id;
    IF v_user3_id IS NULL THEN SELECT Id INTO v_user3_id FROM Users WHERE Email = 'robert.williams@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_user3_id, v_user_role_id) ON CONFLICT DO NOTHING;
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('jennifer.brown@company.com', v_test_hash, v_test_salt, 'Jennifer', 'Brown', 'Jennifer Brown', '0430-444-444', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_user4_id;
    IF v_user4_id IS NULL THEN SELECT Id INTO v_user4_id FROM Users WHERE Email = 'jennifer.brown@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_user4_id, v_user_role_id) ON CONFLICT DO NOTHING;
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('daniel.miller@company.com', v_test_hash, v_test_salt, 'Daniel', 'Miller', 'Daniel Miller', '0430-555-555', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_user5_id;
    IF v_user5_id IS NULL THEN SELECT Id INTO v_user5_id FROM Users WHERE Email = 'daniel.miller@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_user5_id, v_user_role_id) ON CONFLICT DO NOTHING;
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('lisa.anderson@company.com', v_test_hash, v_test_salt, 'Lisa', 'Anderson', 'Lisa Anderson', '0430-666-666', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_user6_id;
    IF v_user6_id IS NULL THEN SELECT Id INTO v_user6_id FROM Users WHERE Email = 'lisa.anderson@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_user6_id, v_user_role_id) ON CONFLICT DO NOTHING;
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('matthew.thomas@company.com', v_test_hash, v_test_salt, 'Matthew', 'Thomas', 'Matthew Thomas', '0430-777-777', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_user7_id;
    IF v_user7_id IS NULL THEN SELECT Id INTO v_user7_id FROM Users WHERE Email = 'matthew.thomas@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_user7_id, v_user_role_id) ON CONFLICT DO NOTHING;
    
    INSERT INTO Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, DisplayName, Phone, IsActive)
    VALUES ('amanda.jackson@company.com', v_test_hash, v_test_salt, 'Amanda', 'Jackson', 'Amanda Jackson', '0430-888-888', TRUE)
    ON CONFLICT (Email) DO NOTHING
    RETURNING Id INTO v_user8_id;
    IF v_user8_id IS NULL THEN SELECT Id INTO v_user8_id FROM Users WHERE Email = 'amanda.jackson@company.com'; END IF;
    INSERT INTO UserRoles (UserId, RoleId) VALUES (v_user8_id, v_user_role_id) ON CONFLICT DO NOTHING;

    -- =====================================================
    -- CREATE TICKETS (Various states for reporting)
    -- =====================================================
    
    -- NEW TICKETS (5 tickets)
    INSERT INTO Tickets (TicketNumber, Title, Description, CategoryId, PriorityId, StatusId, RequesterId, AssignedTeamId, CreatedAt, Source)
    VALUES 
        ('TKT-2025-00001', 'Cannot connect to VPN from home', 'I am working from home and unable to establish a VPN connection. I get an error saying "Connection timed out".', v_cat_network, v_priority_high, v_status_new, v_user1_id, v_network_team_id, NOW() - INTERVAL '2 hours', 'Web'),
        ('TKT-2025-00002', 'Need access to SharePoint site', 'Please grant me access to the Marketing SharePoint site for the new campaign.', v_cat_account, v_priority_medium, v_status_new, v_user2_id, v_it_support_id, NOW() - INTERVAL '1 hour', 'Web'),
        ('TKT-2025-00003', 'Laptop running very slow', 'My laptop has been running extremely slowly for the past week. Applications take forever to open.', v_cat_hardware, v_priority_medium, v_status_new, v_user3_id, v_it_support_id, NOW() - INTERVAL '30 minutes', 'Web'),
        ('TKT-2025-00004', 'Excel crashes when opening large files', 'Whenever I try to open spreadsheets larger than 50MB, Excel crashes immediately.', v_cat_software, v_priority_high, v_status_new, v_user4_id, v_app_support_id, NOW() - INTERVAL '15 minutes', 'Email'),
        ('TKT-2025-00005', 'Request for second monitor', 'I would like to request a second monitor for my workstation to improve productivity.', v_cat_hardware, v_priority_low, v_status_new, v_user5_id, v_helpdesk_id, NOW() - INTERVAL '5 minutes', 'Web')
    ON CONFLICT (TicketNumber) DO NOTHING;
    
    -- OPEN TICKETS (4 tickets)
    INSERT INTO Tickets (TicketNumber, Title, Description, CategoryId, PriorityId, StatusId, RequesterId, AssignedToId, AssignedTeamId, CreatedAt, FirstResponseAt, Source)
    VALUES 
        ('TKT-2025-00006', 'Email not syncing on mobile', 'My work email stopped syncing on my phone yesterday. I have tried removing and re-adding the account.', v_cat_software, v_priority_medium, v_status_open, v_user1_id, v_agent7_id, v_helpdesk_id, NOW() - INTERVAL '1 day', NOW() - INTERVAL '20 hours', 'Web'),
        ('TKT-2025-00007', 'Printer not working on 3rd floor', 'The main printer on the 3rd floor is showing offline status and nobody can print.', v_cat_hardware, v_priority_high, v_status_open, v_user6_id, v_agent1_id, v_it_support_id, NOW() - INTERVAL '4 hours', NOW() - INTERVAL '3 hours', 'Phone'),
        ('TKT-2025-00008', 'Need software installation - Adobe Creative Suite', 'I need Adobe Creative Suite installed on my computer for design work. I have manager approval.', v_cat_software, v_priority_medium, v_status_open, v_user7_id, v_agent2_id, v_it_support_id, NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day 20 hours', 'Web'),
        ('TKT-2025-00009', 'WiFi keeps disconnecting in meeting room B', 'The WiFi connection in meeting room B is unstable. It disconnects every 10-15 minutes during meetings.', v_cat_network, v_priority_high, v_status_open, v_user8_id, v_agent3_id, v_network_team_id, NOW() - INTERVAL '6 hours', NOW() - INTERVAL '5 hours', 'Web')
    ON CONFLICT (TicketNumber) DO NOTHING;
    
    -- IN PROGRESS TICKETS (5 tickets)
    INSERT INTO Tickets (TicketNumber, Title, Description, CategoryId, PriorityId, StatusId, RequesterId, AssignedToId, AssignedTeamId, CreatedAt, FirstResponseAt, Source)
    VALUES 
        ('TKT-2025-00010', 'Server response time too slow', 'The internal application server is responding very slowly, affecting all users.', v_cat_network, v_priority_critical, v_status_in_progress, v_user2_id, v_agent4_id, v_network_team_id, NOW() - INTERVAL '3 hours', NOW() - INTERVAL '2 hours 50 minutes', 'Phone'),
        ('TKT-2025-00011', 'Password reset for multiple users', 'Need to reset passwords for 5 users in the finance department after the security audit.', v_cat_account, v_priority_high, v_status_in_progress, v_user3_id, v_agent8_id, v_helpdesk_id, NOW() - INTERVAL '5 hours', NOW() - INTERVAL '4 hours 30 minutes', 'Email'),
        ('TKT-2025-00012', 'Database connection errors in CRM', 'The CRM system is showing intermittent database connection errors for several users.', v_cat_software, v_priority_critical, v_status_in_progress, v_user4_id, v_agent5_id, v_app_support_id, NOW() - INTERVAL '2 hours', NOW() - INTERVAL '1 hour 45 minutes', 'Web'),
        ('TKT-2025-00013', 'New employee workstation setup', 'Please set up a workstation for the new employee starting next Monday in the Sales department.', v_cat_hardware, v_priority_medium, v_status_in_progress, v_user5_id, v_agent1_id, v_it_support_id, NOW() - INTERVAL '3 days', NOW() - INTERVAL '2 days 22 hours', 'Web'),
        ('TKT-2025-00014', 'ERP module not loading correctly', 'The inventory module in the ERP system is not loading any data. Shows a blank screen.', v_cat_software, v_priority_high, v_status_in_progress, v_user6_id, v_agent6_id, v_app_support_id, NOW() - INTERVAL '8 hours', NOW() - INTERVAL '7 hours', 'Web')
    ON CONFLICT (TicketNumber) DO NOTHING;
    
    -- PENDING TICKETS (3 tickets - waiting for customer)
    INSERT INTO Tickets (TicketNumber, Title, Description, CategoryId, PriorityId, StatusId, RequesterId, AssignedToId, AssignedTeamId, CreatedAt, FirstResponseAt, Source)
    VALUES 
        ('TKT-2025-00015', 'Cannot access shared drive', 'I am unable to access the finance shared drive. Access denied error appears.', v_cat_account, v_priority_medium, v_status_pending, v_user7_id, v_agent7_id, v_helpdesk_id, NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day 22 hours', 'Web'),
        ('TKT-2025-00016', 'Outlook add-in causing crashes', 'One of the Outlook add-ins is causing the application to crash on startup.', v_cat_software, v_priority_medium, v_status_pending, v_user8_id, v_agent2_id, v_it_support_id, NOW() - INTERVAL '4 days', NOW() - INTERVAL '3 days 20 hours', 'Email'),
        ('TKT-2025-00017', 'Request for remote access token', 'I need a new RSA token for remote access. My current one is expiring next week.', v_cat_account, v_priority_low, v_status_pending, v_user1_id, v_agent8_id, v_helpdesk_id, NOW() - INTERVAL '1 day', NOW() - INTERVAL '20 hours', 'Web')
    ON CONFLICT (TicketNumber) DO NOTHING;
    
    -- ON HOLD TICKETS (2 tickets)
    INSERT INTO Tickets (TicketNumber, Title, Description, CategoryId, PriorityId, StatusId, RequesterId, AssignedToId, AssignedTeamId, CreatedAt, FirstResponseAt, Source)
    VALUES 
        ('TKT-2025-00018', 'Server migration for legacy app', 'Need to migrate the legacy HR application to the new server infrastructure.', v_cat_software, v_priority_medium, v_status_on_hold, v_user2_id, v_agent5_id, v_app_support_id, NOW() - INTERVAL '7 days', NOW() - INTERVAL '6 days 22 hours', 'Web'),
        ('TKT-2025-00019', 'Network switch replacement needed', 'The network switch in building C needs to be replaced due to frequent failures.', v_cat_network, v_priority_high, v_status_on_hold, v_user3_id, v_agent4_id, v_network_team_id, NOW() - INTERVAL '10 days', NOW() - INTERVAL '9 days 20 hours', 'Phone')
    ON CONFLICT (TicketNumber) DO NOTHING;
    
    -- RESOLVED TICKETS (8 tickets)
    INSERT INTO Tickets (TicketNumber, Title, Description, CategoryId, PriorityId, StatusId, RequesterId, AssignedToId, AssignedTeamId, CreatedAt, FirstResponseAt, ResolvedAt, Source)
    VALUES 
        ('TKT-2025-00020', 'Password reset request', 'Please reset my password. I forgot it after the holiday break.', v_cat_account, v_priority_medium, v_status_resolved, v_user4_id, v_agent7_id, v_helpdesk_id, NOW() - INTERVAL '5 days', NOW() - INTERVAL '4 days 23 hours', NOW() - INTERVAL '4 days 22 hours', 'Web'),
        ('TKT-2025-00021', 'Laptop keyboard not working', 'Several keys on my laptop keyboard stopped working.', v_cat_hardware, v_priority_medium, v_status_resolved, v_user5_id, v_agent1_id, v_it_support_id, NOW() - INTERVAL '6 days', NOW() - INTERVAL '5 days 20 hours', NOW() - INTERVAL '3 days', 'Web'),
        ('TKT-2025-00022', 'Cannot print to network printer', 'I cannot print to the network printer from my computer. Other printers work fine.', v_cat_hardware, v_priority_low, v_status_resolved, v_user6_id, v_agent2_id, v_it_support_id, NOW() - INTERVAL '4 days', NOW() - INTERVAL '3 days 22 hours', NOW() - INTERVAL '2 days', 'Phone'),
        ('TKT-2025-00023', 'Slow internet connection', 'My internet connection at my desk is very slow compared to other areas.', v_cat_network, v_priority_medium, v_status_resolved, v_user7_id, v_agent3_id, v_network_team_id, NOW() - INTERVAL '3 days', NOW() - INTERVAL '2 days 20 hours', NOW() - INTERVAL '1 day', 'Web'),
        ('TKT-2025-00024', 'Software license renewal', 'My AutoCAD license has expired and I need it renewed for my project work.', v_cat_software, v_priority_high, v_status_resolved, v_user8_id, v_agent5_id, v_app_support_id, NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day 22 hours', NOW() - INTERVAL '1 day', 'Email'),
        ('TKT-2025-00025', 'New user account creation', 'Please create an account for new employee starting in IT department.', v_cat_account, v_priority_medium, v_status_resolved, v_user1_id, v_agent8_id, v_helpdesk_id, NOW() - INTERVAL '8 days', NOW() - INTERVAL '7 days 22 hours', NOW() - INTERVAL '6 days', 'Web'),
        ('TKT-2025-00026', 'Monitor flickering issue', 'My monitor keeps flickering intermittently throughout the day.', v_cat_hardware, v_priority_medium, v_status_resolved, v_user2_id, v_agent1_id, v_it_support_id, NOW() - INTERVAL '9 days', NOW() - INTERVAL '8 days 20 hours', NOW() - INTERVAL '7 days', 'Web'),
        ('TKT-2025-00027', 'VPN configuration for contractor', 'Need to configure VPN access for a contractor working remotely.', v_cat_network, v_priority_medium, v_status_resolved, v_user3_id, v_agent4_id, v_network_team_id, NOW() - INTERVAL '5 days', NOW() - INTERVAL '4 days 22 hours', NOW() - INTERVAL '3 days', 'Phone')
    ON CONFLICT (TicketNumber) DO NOTHING;
    
    -- CLOSED TICKETS (10 tickets - older tickets)
    INSERT INTO Tickets (TicketNumber, Title, Description, CategoryId, PriorityId, StatusId, RequesterId, AssignedToId, AssignedTeamId, CreatedAt, FirstResponseAt, ResolvedAt, ClosedAt, Source)
    VALUES 
        ('TKT-2025-00028', 'Office 365 activation issue', 'Cannot activate Office 365 on my new laptop.', v_cat_software, v_priority_medium, v_status_closed, v_user4_id, v_agent2_id, v_it_support_id, NOW() - INTERVAL '15 days', NOW() - INTERVAL '14 days 22 hours', NOW() - INTERVAL '13 days', NOW() - INTERVAL '12 days', 'Web'),
        ('TKT-2025-00029', 'Email storage quota exceeded', 'My mailbox is full and I cannot receive new emails.', v_cat_software, v_priority_high, v_status_closed, v_user5_id, v_agent7_id, v_helpdesk_id, NOW() - INTERVAL '20 days', NOW() - INTERVAL '19 days 20 hours', NOW() - INTERVAL '19 days', NOW() - INTERVAL '18 days', 'Email'),
        ('TKT-2025-00030', 'Backup restoration request', 'Need to restore files from backup that were accidentally deleted.', v_cat_other, v_priority_critical, v_status_closed, v_user6_id, v_agent5_id, v_app_support_id, NOW() - INTERVAL '12 days', NOW() - INTERVAL '11 days 23 hours', NOW() - INTERVAL '11 days', NOW() - INTERVAL '10 days', 'Phone'),
        ('TKT-2025-00031', 'Meeting room display not working', 'The display screen in meeting room A is not showing any output.', v_cat_hardware, v_priority_medium, v_status_closed, v_user7_id, v_agent1_id, v_it_support_id, NOW() - INTERVAL '18 days', NOW() - INTERVAL '17 days 22 hours', NOW() - INTERVAL '16 days', NOW() - INTERVAL '15 days', 'Web'),
        ('TKT-2025-00032', 'File server access issue', 'Cannot access certain folders on the file server.', v_cat_network, v_priority_medium, v_status_closed, v_user8_id, v_agent3_id, v_network_team_id, NOW() - INTERVAL '25 days', NOW() - INTERVAL '24 days 20 hours', NOW() - INTERVAL '23 days', NOW() - INTERVAL '22 days', 'Web'),
        ('TKT-2025-00033', 'New software installation request', 'Please install Visual Studio Code on my workstation.', v_cat_software, v_priority_low, v_status_closed, v_user1_id, v_agent2_id, v_it_support_id, NOW() - INTERVAL '30 days', NOW() - INTERVAL '29 days 22 hours', NOW() - INTERVAL '28 days', NOW() - INTERVAL '27 days', 'Web'),
        ('TKT-2025-00034', 'Security certificate expired', 'The internal portal shows a security certificate warning.', v_cat_network, v_priority_high, v_status_closed, v_user2_id, v_agent4_id, v_network_team_id, NOW() - INTERVAL '22 days', NOW() - INTERVAL '21 days 22 hours', NOW() - INTERVAL '20 days', NOW() - INTERVAL '19 days', 'Email'),
        ('TKT-2025-00035', 'Outlook calendar sync issue', 'My Outlook calendar is not syncing properly with my mobile device.', v_cat_software, v_priority_medium, v_status_closed, v_user3_id, v_agent8_id, v_helpdesk_id, NOW() - INTERVAL '14 days', NOW() - INTERVAL '13 days 20 hours', NOW() - INTERVAL '12 days', NOW() - INTERVAL '11 days', 'Web'),
        ('TKT-2025-00036', 'Laptop battery replacement', 'My laptop battery no longer holds a charge and needs replacement.', v_cat_hardware, v_priority_low, v_status_closed, v_user4_id, v_agent1_id, v_it_support_id, NOW() - INTERVAL '28 days', NOW() - INTERVAL '27 days 22 hours', NOW() - INTERVAL '20 days', NOW() - INTERVAL '19 days', 'Phone'),
        ('TKT-2025-00037', 'Antivirus update failed', 'The antivirus software on my computer failed to update and shows error.', v_cat_software, v_priority_high, v_status_closed, v_user5_id, v_agent2_id, v_it_support_id, NOW() - INTERVAL '16 days', NOW() - INTERVAL '15 days 22 hours', NOW() - INTERVAL '14 days', NOW() - INTERVAL '13 days', 'Web')
    ON CONFLICT (TicketNumber) DO NOTHING;
    
    -- SLA BREACHED TICKETS (3 tickets to show in SLA compliance reports)
    INSERT INTO Tickets (TicketNumber, Title, Description, CategoryId, PriorityId, StatusId, RequesterId, AssignedToId, AssignedTeamId, CreatedAt, FirstResponseAt, ResolvedAt, ClosedAt, SlaBreached, Source)
    VALUES 
        ('TKT-2025-00038', 'Critical server down - DELAYED', 'Production server went down affecting all operations.', v_cat_network, v_priority_critical, v_status_closed, v_user6_id, v_agent3_id, v_network_team_id, NOW() - INTERVAL '21 days', NOW() - INTERVAL '20 days 18 hours', NOW() - INTERVAL '20 days', NOW() - INTERVAL '19 days', TRUE, 'Phone'),
        ('TKT-2025-00039', 'Urgent security patch needed - DELAYED', 'Critical security vulnerability needs immediate patching.', v_cat_software, v_priority_critical, v_status_closed, v_user7_id, v_agent5_id, v_app_support_id, NOW() - INTERVAL '17 days', NOW() - INTERVAL '16 days 16 hours', NOW() - INTERVAL '15 days', NOW() - INTERVAL '14 days', TRUE, 'Email'),
        ('TKT-2025-00040', 'System-wide email issue - DELAYED', 'Email system down for entire department.', v_cat_software, v_priority_high, v_status_closed, v_user8_id, v_agent7_id, v_helpdesk_id, NOW() - INTERVAL '19 days', NOW() - INTERVAL '18 days 12 hours', NOW() - INTERVAL '16 days', NOW() - INTERVAL '15 days', TRUE, 'Phone')
    ON CONFLICT (TicketNumber) DO NOTHING;

    RAISE NOTICE 'Test data created successfully!';
    RAISE NOTICE 'Users created: 3 Managers, 8 Agents, 8 Regular Users';
    RAISE NOTICE 'Tickets created: 40 total across all statuses';
    
END $$;

-- Update sequence if needed
SELECT setval('tickets_id_seq', (SELECT COALESCE(MAX(id), 0) + 1 FROM Tickets), false);
