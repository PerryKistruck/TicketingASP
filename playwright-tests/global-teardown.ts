import { FullConfig } from '@playwright/test';
import { Client } from 'pg';

/**
 * Configuration for cleanup methods
 */
const CONFIG = {
  // API endpoint for cleanup (used in CI/CD or Azure)
  apiBaseUrl: process.env.BASE_URL || process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5050',
  apiKey: process.env.TEST_CLEANUP_API_KEY || 'test-cleanup-key-change-in-production',
  
  // Database connection (used for local development with SSH tunnel)
  database: {
    host: 'localhost',
    port: 5433,
    database: 'webapp-ticket-database',
    user: 'ticketadmin',
    password: process.env.DB_PASSWORD || 'Ticketing@123!',
    ssl: false,
  }
};

/**
 * Global teardown - runs once after all tests complete
 * Tries API cleanup first (works in CI/CD), falls back to direct DB (local dev)
 */
async function globalTeardown(config: FullConfig) {
  console.log('\n[Cleanup] Running global cleanup...');
  
  // Try API cleanup first (works in CI/CD and Azure)
  const apiSuccess = await tryApiCleanup();
  
  if (!apiSuccess) {
    // Fall back to direct DB cleanup (for local development with SSH tunnel)
    console.log('   Falling back to direct database cleanup...');
    await tryDatabaseCleanup();
  }
}

/**
 * Attempt cleanup via API endpoint
 * This works in CI/CD pipelines where the app is running in Azure
 */
async function tryApiCleanup(): Promise<boolean> {
  try {
    console.log(`   Trying API cleanup at ${CONFIG.apiBaseUrl}/api/TestCleanup/cleanup`);
    
    const response = await fetch(`${CONFIG.apiBaseUrl}/api/TestCleanup/cleanup`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Test-Api-Key': CONFIG.apiKey,
      },
    });
    
    if (!response.ok) {
      console.log(`   [Warning] API cleanup failed with status ${response.status}`);
      return false;
    }
    
    const result = await response.json() as CleanupResult;
    
    if (result.success) {
      console.log('[OK] API Cleanup successful:');
      console.log(`   - Deleted tickets: ${result.deletedTickets}`);
      console.log(`   - Deleted team members: ${result.deletedTeamMembers}`);
      console.log(`   - Deleted teams: ${result.deletedTeams}`);
      console.log(`   - Deleted users: ${result.deletedUsers}`);
      console.log(`   - Unlocked users: ${result.unlockedUsers}`);
      return true;
    } else {
      console.log(`   [Warning] API cleanup reported failure: ${result.message}`);
      return false;
    }
  } catch (error) {
    console.log(`   [Warning] API cleanup unavailable: ${error}`);
    return false;
  }
}

interface CleanupResult {
  success: boolean;
  message: string;
  deletedTickets: number;
  deletedTeamMembers: number;
  deletedTeams: number;
  deletedUsers: number;
  unlockedUsers: number;
}

/**
 * Attempt cleanup via direct database connection
 * This works for local development with SSH tunnel running
 */
async function tryDatabaseCleanup(): Promise<boolean> {
  const client = new Client(CONFIG.database);
  
  try {
    await client.connect();
    console.log('   [OK] Connected to database');
    
    // Run comprehensive cleanup
    await client.query(`
      DO $$
      DECLARE
          v_test_hash VARCHAR(255) := 'Epuwo9diXAACngEJUbp+KlOGRUYJr76MtCmix4nYIv9Fb5HdH5FdIudOv7t3dyyKG1ZaZ8aqOsL9HNebbgt0xQ==';
          v_test_salt VARCHAR(255) := '8Pdi6g8w2skQBXA4D1edjs7PQJj7FyH3q7ID7h2Ze7CYDUdTukN5MRsjFWWsu2PFseElpjuuXvemP+5dBU4rMDOCx2V10fS+gYuBQLKIFF/0XSjWHbClENk6uZ9M0SO6NXoCPeCWP4duyuy9mtPN3fUb51WBuodu6kk1/TFYC38=';
          v_deleted_tickets INT := 0;
          v_deleted_teams INT := 0;
          v_deleted_users INT := 0;
          v_deleted_teammembers INT := 0;
          v_unlocked_users INT := 0;
      BEGIN
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
          
          -- =====================================================
          -- 2. DELETE TEAM MEMBERS FOR TEST TEAMS
          -- =====================================================
          
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
          
          -- =====================================================
          -- 3. DELETE TEST TEAMS
          -- =====================================================
          
          DELETE FROM teams 
          WHERE name LIKE '%E2E Test%'
             OR name LIKE '%Test Team%'
             OR name LIKE '%Audit Test%'
             OR name LIKE '%<script>%'
             OR name LIKE '%SELECT%'
             OR name LIKE '%DROP%'
             OR description LIKE '%E2E%';
          
          GET DIAGNOSTICS v_deleted_teams = ROW_COUNT;
          
          -- =====================================================
          -- 4. DELETE TEST USERS (SQL injection, XSS patterns)
          -- =====================================================
          
          -- First delete from userroles
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
          
          -- Then delete from teammembers
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
          
          -- Delete from auditlogs
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
          
          -- Delete from usersessions
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
          
          -- Finally delete users
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
          
          -- =====================================================
          -- 5. UNLOCK AND RESET PASSWORDS FOR ALL TEST USERS
          -- =====================================================
          
          UPDATE users 
          SET passwordhash = v_test_hash,
              passwordsalt = v_test_salt,
              isactive = TRUE,
              islocked = FALSE,
              failedloginattempts = 0
          WHERE email LIKE '%@company.com'
             OR email = 'john.smith@testcorp.com';
          
          GET DIAGNOSTICS v_unlocked_users = ROW_COUNT;
          
          RAISE NOTICE 'Cleanup complete - Tickets: %, Team members: %, Teams: %, Users: %, Unlocked: %', 
                       v_deleted_tickets, v_deleted_teammembers, v_deleted_teams, v_deleted_users, v_unlocked_users;
          
      END $$;
    `);
    
    // Get cleanup statistics
    const stats = await client.query(`
      SELECT 
        (SELECT COUNT(*) FROM teams WHERE name LIKE '%E2E%' OR name LIKE '%Test Team%') as remaining_test_teams,
        (SELECT COUNT(*) FROM tickets WHERE title LIKE '%E2E%' OR title LIKE '%Test Ticket%') as remaining_test_tickets,
        (SELECT COUNT(*) FROM users WHERE islocked = TRUE) as locked_users
    `);
    
    const row = stats.rows[0];
    console.log(`   Cleanup verification:`);
    console.log(`     - Remaining test teams: ${row.remaining_test_teams}`);
    console.log(`     - Remaining test tickets: ${row.remaining_test_tickets}`);
    console.log(`     - Locked users: ${row.locked_users}`);
    
    if (row.remaining_test_teams === '0' && row.remaining_test_tickets === '0' && row.locked_users === '0') {
      console.log('[OK] Database fully cleaned up');
    } else {
      console.log('[Warning] Some test data may remain');
    }
    
    return true;
    
  } catch (error) {
    console.log(`   [Warning] SQL Cleanup error: ${error}`);
    console.log('      Make sure the SSH tunnel is running for database access');
    return false;
  } finally {
    await client.end();
  }
}

export default globalTeardown;
