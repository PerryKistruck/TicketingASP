-- =====================================================
-- CLEANUP TEST DATA AND RESET PASSWORDS
-- Description: Removes test artifacts and resets all test user passwords to "Test@123"
-- Run this script to clean up after E2E test runs
-- Table names are lowercase (PostgreSQL convention)
-- =====================================================

DO $$
DECLARE
    v_test_hash VARCHAR(255) := 'Epuwo9diXAACngEJUbp+KlOGRUYJr76MtCmix4nYIv9Fb5HdH5FdIudOv7t3dyyKG1ZaZ8aqOsL9HNebbgt0xQ==';
    v_test_salt VARCHAR(255) := '8Pdi6g8w2skQBXA4D1edjs7PQJj7FyH3q7ID7h2Ze7CYDUdTukN5MRsjFWWsu2PFseElpjuuXvemP+5dBU4rMDOCx2V10fS+gYuBQLKIFF/0XSjWHbClENk6uZ9M0SO6NXoCPeCWP4duyuy9mtPN3fUb51WBuodu6kk1/TFYC38=';
    v_deleted_users INT := 0;
    v_deleted_tickets INT := 0;
    v_deleted_teams INT := 0;
    v_deleted_teammembers INT := 0;
    v_updated_passwords INT := 0;
BEGIN
    RAISE NOTICE '=== Starting Test Data Cleanup ===';
    
    -- =====================================================
    -- 1. DELETE TEST TICKETS (by pattern in title)
    -- =====================================================
    
    DELETE FROM tickets 
    WHERE title LIKE '%E2E Test%'
       OR title LIKE '%Test Ticket%'
       OR title LIKE '%XSS Test%'
       OR title LIKE '%List Test%'
       OR title LIKE '%Workflow Test%'
       OR title LIKE '%Test CSRF%'
       OR title LIKE '%SELECT%FROM%'
       OR title LIKE '%DROP%TABLE%'
       OR title LIKE '%UNION%SELECT%'
       OR title LIKE '%OR ''1''=''1%'
       OR title LIKE '%'';%--'
       OR title LIKE '%DELETE%FROM%'
       OR title LIKE '%INSERT%INTO%'
       OR title LIKE '%UPDATE%SET%'
       OR title LIKE '%<script>%'
       OR title LIKE '%onerror=%'
       OR title LIKE '%javascript:%'
       OR title LIKE '%Normal Title%';
    
    GET DIAGNOSTICS v_deleted_tickets = ROW_COUNT;
    RAISE NOTICE 'Deleted % test tickets (by title pattern)', v_deleted_tickets;
    
    -- =====================================================
    -- 1b. DELETE TEST TEAMS AND TEAM MEMBERS
    -- =====================================================
    
    -- Delete team members first (foreign key constraint)
    DELETE FROM teammembers 
    WHERE teamid IN (
        SELECT id FROM teams 
        WHERE name LIKE '%E2E Test%'
           OR name LIKE '%Test Team%'
           OR name LIKE '%Audit Test%'
           OR name LIKE '%<script>%'
           OR name LIKE '%SELECT%'
           OR name LIKE '%DROP%'
           OR description LIKE '%E2E%'
    );
    
    GET DIAGNOSTICS v_deleted_teammembers = ROW_COUNT;
    RAISE NOTICE 'Deleted % team members from test teams', v_deleted_teammembers;
    
    -- Delete test teams
    DELETE FROM teams 
    WHERE name LIKE '%E2E Test%'
       OR name LIKE '%Test Team%'
       OR name LIKE '%Audit Test%'
       OR name LIKE '%<script>%'
       OR name LIKE '%SELECT%'
       OR name LIKE '%DROP%'
       OR description LIKE '%E2E%';
    
    GET DIAGNOSTICS v_deleted_teams = ROW_COUNT;
    RAISE NOTICE 'Deleted % test teams', v_deleted_teams;
    
    -- =====================================================
    -- 2. DELETE TICKETS CREATED BY TEST USERS
    -- =====================================================
    
    DELETE FROM tickets 
    WHERE requesterid IN (
        SELECT id FROM users 
        WHERE email LIKE '%test%@test.com'
           OR email LIKE '%'' OR%'
           OR email LIKE '%SELECT%'
           OR email LIKE '%DROP%'
           OR email LIKE '%UNION%'
           OR email LIKE '%DELETE%'
           OR email LIKE '%;--%'
           OR email LIKE '%<script>%'
           OR firstname LIKE '%SELECT%'
           OR firstname LIKE '%DROP%'
           OR firstname LIKE '%UNION%'
           OR firstname LIKE '%'';%'
           OR firstname LIKE '%OR ''1''=%'
           OR firstname LIKE '%<script>%'
           OR firstname LIKE '%onerror=%'
           OR lastname = 'TestUser'
           OR displayname LIKE '%Test User%'
    );
    
    GET DIAGNOSTICS v_deleted_tickets = ROW_COUNT;
    RAISE NOTICE 'Deleted % tickets from test users', v_deleted_tickets;
    
    -- =====================================================
    -- 3. DELETE AUDIT LOGS FOR TEST USERS
    -- =====================================================
    
    DELETE FROM auditlogs 
    WHERE userid IN (
        SELECT id FROM users 
        WHERE email LIKE '%test%@test.com'
           OR email LIKE '%'' OR%'
           OR email LIKE '%SELECT%'
           OR email LIKE '%DROP%'
           OR email LIKE '%UNION%'
           OR email LIKE '%DELETE%'
           OR email LIKE '%;--%'
           OR email LIKE '%<script>%'
           OR firstname LIKE '%SELECT%'
           OR firstname LIKE '%DROP%'
           OR firstname LIKE '%UNION%'
           OR firstname LIKE '%'';%'
           OR firstname LIKE '%OR ''1''=%'
           OR firstname LIKE '%<script>%'
           OR firstname LIKE '%onerror=%'
           OR lastname = 'TestUser'
           OR displayname LIKE '%Test User%'
    );
    
    RAISE NOTICE 'Deleted audit logs for test users';
    
    -- =====================================================
    -- 4. DELETE USER SESSIONS FOR TEST USERS
    -- =====================================================
    
    DELETE FROM usersessions 
    WHERE userid IN (
        SELECT id FROM users 
        WHERE email LIKE '%test%@test.com'
           OR email LIKE '%'' OR%'
           OR email LIKE '%SELECT%'
           OR email LIKE '%DROP%'
           OR email LIKE '%UNION%'
           OR email LIKE '%DELETE%'
           OR email LIKE '%;--%'
           OR email LIKE '%<script>%'
           OR firstname LIKE '%SELECT%'
           OR firstname LIKE '%DROP%'
           OR firstname LIKE '%UNION%'
           OR firstname LIKE '%'';%'
           OR firstname LIKE '%OR ''1''=%'
           OR firstname LIKE '%<script>%'
           OR firstname LIKE '%onerror=%'
           OR lastname = 'TestUser'
           OR displayname LIKE '%Test User%'
    );
    
    RAISE NOTICE 'Deleted sessions for test users';
    
    -- =====================================================
    -- 5. DELETE TEST USERS
    -- =====================================================
    
    DELETE FROM userroles 
    WHERE userid IN (
        SELECT id FROM users 
        WHERE email LIKE '%test%@test.com'
           OR email LIKE '%'' OR%'
           OR email LIKE '%SELECT%'
           OR email LIKE '%DROP%'
           OR email LIKE '%UNION%'
           OR email LIKE '%DELETE%'
           OR email LIKE '%;--%'
           OR email LIKE '%<script>%'
           OR firstname LIKE '%SELECT%'
           OR firstname LIKE '%DROP%'
           OR firstname LIKE '%UNION%'
           OR firstname LIKE '%'';%'
           OR firstname LIKE '%OR ''1''=%'
           OR firstname LIKE '%<script>%'
           OR firstname LIKE '%onerror=%'
           OR lastname = 'TestUser'
           OR displayname LIKE '%Test User%'
    );
    
    DELETE FROM teammembers 
    WHERE userid IN (
        SELECT id FROM users 
        WHERE email LIKE '%test%@test.com'
           OR email LIKE '%'' OR%'
           OR email LIKE '%SELECT%'
           OR email LIKE '%DROP%'
           OR email LIKE '%UNION%'
           OR email LIKE '%DELETE%'
           OR email LIKE '%;--%'
           OR email LIKE '%<script>%'
           OR firstname LIKE '%SELECT%'
           OR firstname LIKE '%DROP%'
           OR firstname LIKE '%UNION%'
           OR firstname LIKE '%'';%'
           OR firstname LIKE '%OR ''1''=%'
           OR firstname LIKE '%<script>%'
           OR firstname LIKE '%onerror=%'
           OR lastname = 'TestUser'
           OR displayname LIKE '%Test User%'
    );
    
    DELETE FROM users 
    WHERE email LIKE '%test%@test.com'
       OR email LIKE '%'' OR%'
       OR email LIKE '%SELECT%'
       OR email LIKE '%DROP%'
       OR email LIKE '%UNION%'
       OR email LIKE '%DELETE%'
       OR email LIKE '%;--%'
       OR email LIKE '%<script>%'
       OR firstname LIKE '%SELECT%'
       OR firstname LIKE '%DROP%'
       OR firstname LIKE '%UNION%'
       OR firstname LIKE '%'';%'
       OR firstname LIKE '%OR ''1''=%'
       OR firstname LIKE '%<script>%'
       OR firstname LIKE '%onerror=%'
       OR lastname = 'TestUser'
       OR displayname LIKE '%Test User%';
    
    GET DIAGNOSTICS v_deleted_users = ROW_COUNT;
    RAISE NOTICE 'Deleted % test users', v_deleted_users;
    
    -- =====================================================
    -- 6. UNLOCK AND RESET PASSWORDS FOR ALL TEST USERS
    -- =====================================================
    
    UPDATE users 
    SET passwordhash = v_test_hash,
        passwordsalt = v_test_salt,
        isactive = TRUE,
        islocked = FALSE,
        failedloginattempts = 0
    WHERE email LIKE '%@company.com'
       OR email = 'john.smith@testcorp.com';
    
    GET DIAGNOSTICS v_updated_passwords = ROW_COUNT;
    RAISE NOTICE 'Reset passwords for % users', v_updated_passwords;
    
    RAISE NOTICE '=== Cleanup Complete ===';
    
END $$;

-- Verify users after cleanup
SELECT email, isactive, islocked, failedloginattempts
FROM users 
WHERE email LIKE '%@company.com'
ORDER BY email;
