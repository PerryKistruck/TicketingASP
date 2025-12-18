using Dapper;
using TicketingASP.Data;
using TicketingASP.Models.DTOs;

namespace TicketingASP.Repositories;

/// <summary>
/// Repository interface for lookup data operations
/// </summary>
public interface ILookupRepository
{
    Task<List<LookupItemDto>> GetPrioritiesAsync();
    Task<List<LookupItemDto>> GetStatusesAsync();
    Task<List<CategoryLookupDto>> GetCategoriesAsync();
    Task<List<LookupItemDto>> GetTeamsAsync();
    Task<List<AgentLookupDto>> GetAgentsAsync();
    Task<List<LookupItemDto>> GetRolesAsync();
}

/// <summary>
/// Implementation of lookup repository using PostgreSQL stored procedures
/// </summary>
public class LookupRepository : ILookupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<LookupRepository> _logger;

    public LookupRepository(IDbConnectionFactory connectionFactory, ILogger<LookupRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<List<LookupItemDto>> GetPrioritiesAsync()
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<LookupItemDto>(
                "SELECT id AS Id, name AS Name, color AS Color FROM sp_lookup_priorities()");

            _logger.LogDebug("Loaded {Count} priorities", result.Count());
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting priorities lookup: {Message}", ex.Message);
            return new List<LookupItemDto>();
        }
    }

    public async Task<List<LookupItemDto>> GetStatusesAsync()
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<LookupItemDto>(
                "SELECT id AS Id, name AS Name, color AS Color FROM sp_lookup_statuses()");

            _logger.LogDebug("Loaded {Count} statuses", result.Count());
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses lookup: {Message}", ex.Message);
            return new List<LookupItemDto>();
        }
    }

    public async Task<List<CategoryLookupDto>> GetCategoriesAsync()
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var allCategories = await connection.QueryAsync<CategoryLookupDto>(
                "SELECT id AS Id, name AS Name, parent_id AS ParentId, parent_name AS ParentName FROM sp_lookup_categories()");

            var categoryList = allCategories.ToList();
            
            // Build hierarchy: separate parents (no ParentId) and children (have ParentId)
            var parentCategories = categoryList.Where(c => c.ParentId == null).ToList();
            var childCategories = categoryList.Where(c => c.ParentId != null).ToList();
            
            // Assign children to their parents
            foreach (var parent in parentCategories)
            {
                parent.SubCategories = childCategories
                    .Where(c => c.ParentId == parent.Id)
                    .ToList();
            }

            _logger.LogDebug("Loaded {Count} categories ({ParentCount} parents, {ChildCount} children)", 
                categoryList.Count, parentCategories.Count, childCategories.Count);
            
            return parentCategories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories lookup: {Message}", ex.Message);
            return new List<CategoryLookupDto>();
        }
    }

    public async Task<List<LookupItemDto>> GetTeamsAsync()
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<LookupItemDto>(
                "SELECT id AS Id, name AS Name FROM sp_lookup_teams()");

            _logger.LogDebug("Loaded {Count} teams", result.Count());
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting teams lookup: {Message}", ex.Message);
            return new List<LookupItemDto>();
        }
    }

    public async Task<List<AgentLookupDto>> GetAgentsAsync()
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<AgentLookupDto>(
                "SELECT id AS Id, display_name AS Name, display_name AS DisplayName, email AS Email FROM sp_lookup_agents()");

            _logger.LogDebug("Loaded {Count} agents", result.Count());
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agents lookup: {Message}", ex.Message);
            return new List<AgentLookupDto>();
        }
    }

    public async Task<List<LookupItemDto>> GetRolesAsync()
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            // Get support staff roles only (Admin=1, Manager=2, Agent=3), excluding User role
            var result = await connection.QueryAsync<LookupItemDto>(
                "SELECT Id, Name FROM Roles WHERE IsActive = true AND Id IN (1, 2, 3) ORDER BY Id");

            _logger.LogDebug("Loaded {Count} roles", result.Count());
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles lookup: {Message}", ex.Message);
            return new List<LookupItemDto>();
        }
    }
}
