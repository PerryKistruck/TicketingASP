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

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting priorities lookup");
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

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses lookup");
            return new List<LookupItemDto>();
        }
    }

    public async Task<List<CategoryLookupDto>> GetCategoriesAsync()
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<CategoryLookupDto>(
                "SELECT id AS Id, name AS Name, parent_id AS ParentId, parent_name AS ParentName FROM sp_lookup_categories()");

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories lookup");
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

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting teams lookup");
            return new List<LookupItemDto>();
        }
    }

    public async Task<List<AgentLookupDto>> GetAgentsAsync()
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryAsync<AgentLookupDto>(
                "SELECT id AS Id, display_name AS DisplayName, email AS Email FROM sp_lookup_agents()");

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agents lookup");
            return new List<AgentLookupDto>();
        }
    }
}
