using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingASP.Data;
using Npgsql;

namespace TicketingASP.Controllers.Api;

/// <summary>
/// API Controller for E2E test cleanup operations
/// Only enabled in Development/Testing environments
/// Protected by API key for CI/CD access
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class TestCleanupController : ControllerBase
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<TestCleanupController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public TestCleanupController(
        IDbConnectionFactory connectionFactory,
        ILogger<TestCleanupController> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Run comprehensive E2E test data cleanup
    /// Removes test tickets, teams, users and resets test accounts
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<IActionResult> RunCleanup([FromHeader(Name = "X-Test-Api-Key")] string? apiKey)
    {
        // Only allow in Development or when proper API key is provided
        var expectedApiKey = _configuration["TestCleanup:ApiKey"] ?? "test-cleanup-key-change-in-production";
        
        if (!_environment.IsDevelopment() && apiKey != expectedApiKey)
        {
            _logger.LogWarning("Unauthorized cleanup attempt");
            return Unauthorized(new { error = "Invalid API key" });
        }

        _logger.LogInformation("Starting E2E test data cleanup");

        var result = new CleanupResult();

        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync();

            // 1. Delete test tickets
            result.DeletedTickets = await DeleteTestTicketsAsync(connection);
            
            // 2. Delete team members for test teams
            result.DeletedTeamMembers = await DeleteTestTeamMembersAsync(connection);
            
            // 3. Delete test teams
            result.DeletedTeams = await DeleteTestTeamsAsync(connection);
            
            // 4. Delete test users (and their related records)
            result.DeletedUsers = await DeleteTestUsersAsync(connection);
            
            // 5. Unlock and reset passwords for all standard test users
            result.UnlockedUsers = await UnlockAndResetTestUsersAsync(connection);

            result.Success = true;
            result.Message = "Cleanup completed successfully";

            _logger.LogInformation("Cleanup complete: {Tickets} tickets, {Teams} teams, {Users} users deleted, {Unlocked} users unlocked",
                result.DeletedTickets, result.DeletedTeams, result.DeletedUsers, result.UnlockedUsers);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup failed");
            result.Success = false;
            result.Message = $"Cleanup failed: {ex.Message}";
            return StatusCode(500, result);
        }
    }

    /// <summary>
    /// Health check endpoint to verify cleanup API is available
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { 
            status = "healthy", 
            environment = _environment.EnvironmentName,
            timestamp = DateTime.UtcNow 
        });
    }

    private async Task<int> DeleteTestTicketsAsync(NpgsqlConnection connection)
    {
        const string sql = @"
            DELETE FROM tickets 
            WHERE title LIKE '%E2E Test%'
               OR title LIKE '%Test Ticket%'
               OR title LIKE '%XSS Test%'               OR title LIKE '%List Test%'
               OR title LIKE '%Workflow Test%'
               OR title LIKE '%Test CSRF%'               OR title LIKE '%SELECT%FROM%'
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
               OR title LIKE '%Normal Title%'";

        using var cmd = new NpgsqlCommand(sql, connection);
        return await cmd.ExecuteNonQueryAsync();
    }

    private async Task<int> DeleteTestTeamMembersAsync(NpgsqlConnection connection)
    {
        const string sql = @"
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
            )";

        using var cmd = new NpgsqlCommand(sql, connection);
        return await cmd.ExecuteNonQueryAsync();
    }

    private async Task<int> DeleteTestTeamsAsync(NpgsqlConnection connection)
    {
        const string sql = @"
            DELETE FROM teams 
            WHERE name LIKE '%E2E Test%'
               OR name LIKE '%Test Team%'
               OR name LIKE '%Audit Test%'
               OR name LIKE '%<script>%'
               OR name LIKE '%SELECT%'
               OR name LIKE '%DROP%'
               OR description LIKE '%E2E%'";

        using var cmd = new NpgsqlCommand(sql, connection);
        return await cmd.ExecuteNonQueryAsync();
    }

    private async Task<int> DeleteTestUsersAsync(NpgsqlConnection connection)
    {
        // Delete from related tables first
        var userCondition = @"
            email LIKE '%test%@test.com'
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
               OR displayname LIKE '%Test User%'";

        // Delete from userroles
        var sql1 = $"DELETE FROM userroles WHERE userid IN (SELECT id FROM users WHERE {userCondition})";
        using (var cmd = new NpgsqlCommand(sql1, connection))
            await cmd.ExecuteNonQueryAsync();

        // Delete from teammembers
        var sql2 = $"DELETE FROM teammembers WHERE userid IN (SELECT id FROM users WHERE {userCondition})";
        using (var cmd = new NpgsqlCommand(sql2, connection))
            await cmd.ExecuteNonQueryAsync();

        // Delete from auditlogs
        var sql3 = $"DELETE FROM auditlogs WHERE userid IN (SELECT id FROM users WHERE {userCondition})";
        using (var cmd = new NpgsqlCommand(sql3, connection))
            await cmd.ExecuteNonQueryAsync();

        // Delete from usersessions
        var sql4 = $"DELETE FROM usersessions WHERE userid IN (SELECT id FROM users WHERE {userCondition})";
        using (var cmd = new NpgsqlCommand(sql4, connection))
            await cmd.ExecuteNonQueryAsync();

        // Finally delete users
        var sql5 = $"DELETE FROM users WHERE {userCondition}";
        using var cmd5 = new NpgsqlCommand(sql5, connection);
        return await cmd5.ExecuteNonQueryAsync();
    }

    private async Task<int> UnlockAndResetTestUsersAsync(NpgsqlConnection connection)
    {
        const string testHash = "Epuwo9diXAACngEJUbp+KlOGRUYJr76MtCmix4nYIv9Fb5HdH5FdIudOv7t3dyyKG1ZaZ8aqOsL9HNebbgt0xQ==";
        const string testSalt = "8Pdi6g8w2skQBXA4D1edjs7PQJj7FyH3q7ID7h2Ze7CYDUdTukN5MRsjFWWsu2PFseElpjuuXvemP+5dBU4rMDOCx2V10fS+gYuBQLKIFF/0XSjWHbClENk6uZ9M0SO6NXoCPeCWP4duyuy9mtPN3fUb51WBuodu6kk1/TFYC38=";

        const string sql = @"
            UPDATE users 
            SET passwordhash = @hash,
                passwordsalt = @salt,
                isactive = TRUE,
                islocked = FALSE,
                failedloginattempts = 0
            WHERE email LIKE '%@company.com'
               OR email = 'john.smith@testcorp.com'";

        using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@hash", testHash);
        cmd.Parameters.AddWithValue("@salt", testSalt);
        return await cmd.ExecuteNonQueryAsync();
    }

    private class CleanupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DeletedTickets { get; set; }
        public int DeletedTeamMembers { get; set; }
        public int DeletedTeams { get; set; }
        public int DeletedUsers { get; set; }
        public int UnlockedUsers { get; set; }
    }
}
