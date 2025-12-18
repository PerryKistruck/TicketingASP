using Dapper;
using TicketingASP.Data;
using TicketingASP.Models.Common;
using TicketingASP.Models.DTOs;

namespace TicketingASP.Repositories;

/// <summary>
/// Repository interface for team operations using stored procedures
/// </summary>
public interface ITeamRepository
{
    Task<OperationResult<int>> CreateAsync(CreateTeamDto dto, int createdBy);
    Task<TeamDto?> GetByIdAsync(int teamId);
    Task<OperationResult> UpdateAsync(int teamId, UpdateTeamDto dto, int updatedBy);
    Task<OperationResult> DeleteAsync(int teamId, int deletedBy);
    Task<PagedResult<TeamDto>> GetListAsync(int pageNumber, int pageSize, string? search = null, bool? isActive = true);
    Task<OperationResult> AddMemberAsync(int teamId, int userId, string role, int addedBy);
    Task<OperationResult> RemoveMemberAsync(int teamId, int userId);
    Task<List<TeamMemberDto>> GetMembersAsync(int teamId);
    Task<List<TeamDto>> GetUserTeamsAsync(int userId);
}

/// <summary>
/// Implementation of team repository using PostgreSQL stored procedures
/// </summary>
public class TeamRepository : ITeamRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<TeamRepository> _logger;

    public TeamRepository(IDbConnectionFactory connectionFactory, ILogger<TeamRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<OperationResult<int>> CreateAsync(CreateTeamDto dto, int createdBy)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_team_create(@p_name, @p_description, @p_manager_id, @p_email, @p_created_by)",
                new
                {
                    p_name = dto.Name,
                    p_description = dto.Description,
                    p_manager_id = dto.ManagerId,
                    p_email = dto.Email,
                    p_created_by = createdBy
                });

            return result?.success == true 
                ? OperationResult<int>.SuccessResult((int)result.team_id, result.message) 
                : OperationResult<int>.FailureResult(result?.message ?? "Failed to create team");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team {TeamName}", dto.Name);
            return OperationResult<int>.FailureResult("An error occurred while creating the team");
        }
    }

    public async Task<TeamDto?> GetByIdAsync(int teamId)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            return await connection.QueryFirstOrDefaultAsync<TeamDto>(
                @"SELECT id, name, description, manager_id AS ManagerId, manager_name AS ManagerName,
                         email, is_active AS IsActive, member_count AS MemberCount, created_at AS CreatedAt
                  FROM sp_team_get_by_id(@p_team_id)",
                new { p_team_id = teamId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team {TeamId}", teamId);
            return null;
        }
    }

    public async Task<OperationResult> UpdateAsync(int teamId, UpdateTeamDto dto, int updatedBy)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_team_update(@p_team_id, @p_name, @p_description, @p_manager_id, @p_email, @p_is_active, @p_updated_by)",
                new
                {
                    p_team_id = teamId,
                    p_name = dto.Name,
                    p_description = dto.Description,
                    p_manager_id = dto.ManagerId,
                    p_email = dto.Email,
                    p_is_active = dto.IsActive,
                    p_updated_by = updatedBy
                });

            return result?.success == true 
                ? OperationResult.SuccessResult(result.message) 
                : OperationResult.FailureResult(result?.message ?? "Failed to update team");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating team {TeamId}", teamId);
            return OperationResult.FailureResult("An error occurred while updating the team");
        }
    }

    public async Task<OperationResult> DeleteAsync(int teamId, int deletedBy)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_team_delete(@p_team_id, @p_deleted_by)",
                new { p_team_id = teamId, p_deleted_by = deletedBy });

            return result?.success == true 
                ? OperationResult.SuccessResult(result.message) 
                : OperationResult.FailureResult(result?.message ?? "Failed to delete team");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting team {TeamId}", teamId);
            return OperationResult.FailureResult("An error occurred while deleting the team");
        }
    }

    public async Task<PagedResult<TeamDto>> GetListAsync(int pageNumber, int pageSize, string? search = null, bool? isActive = true)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var teams = await connection.QueryAsync<TeamDto>(
                @"SELECT id, name, description, manager_name AS ManagerName, email,
                         member_count AS MemberCount, is_active AS IsActive, created_at AS CreatedAt, total_count AS TotalCount
                  FROM sp_team_list(@p_page_number, @p_page_size, @p_search, @p_is_active)",
                new
                {
                    p_page_number = pageNumber,
                    p_page_size = pageSize,
                    p_search = search,
                    p_is_active = isActive
                });

            var teamList = teams.ToList();
            var totalCount = teamList.FirstOrDefault()?.TotalCount ?? 0;

            return new PagedResult<TeamDto>
            {
                Items = teamList,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team list");
            return new PagedResult<TeamDto>();
        }
    }

    public async Task<OperationResult> AddMemberAsync(int teamId, int userId, string role, int addedBy)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_team_add_member(@p_team_id, @p_user_id, @p_role, @p_added_by)",
                new { p_team_id = teamId, p_user_id = userId, p_role = role, p_added_by = addedBy });

            return result?.success == true 
                ? OperationResult.SuccessResult(result.message) 
                : OperationResult.FailureResult(result?.message ?? "Failed to add team member");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member {UserId} to team {TeamId}", userId, teamId);
            return OperationResult.FailureResult("An error occurred while adding the team member");
        }
    }

    public async Task<OperationResult> RemoveMemberAsync(int teamId, int userId)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_team_remove_member(@p_team_id, @p_user_id)",
                new { p_team_id = teamId, p_user_id = userId });

            return result?.success == true 
                ? OperationResult.SuccessResult(result.message) 
                : OperationResult.FailureResult(result?.message ?? "Failed to remove team member");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {UserId} from team {TeamId}", userId, teamId);
            return OperationResult.FailureResult("An error occurred while removing the team member");
        }
    }

    public async Task<List<TeamMemberDto>> GetMembersAsync(int teamId)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var members = await connection.QueryAsync<TeamMemberDto>(
                @"SELECT user_id AS UserId, email, display_name AS DisplayName, role, 
                         CASE WHEN role = 'Leader' THEN true ELSE false END AS IsTeamLead,
                         avatar_url AS AvatarUrl, joined_at AS JoinedAt
                  FROM sp_team_get_members(@p_team_id)",
                new { p_team_id = teamId });

            return members.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members for team {TeamId}", teamId);
            return new List<TeamMemberDto>();
        }
    }

    public async Task<List<TeamDto>> GetUserTeamsAsync(int userId)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var teams = await connection.QueryAsync<TeamDto>(
                @"SELECT team_id AS Id, team_name AS Name, role, member_count AS MemberCount
                  FROM sp_user_get_teams(@p_user_id)",
                new { p_user_id = userId });

            return teams.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting teams for user {UserId}", userId);
            return new List<TeamDto>();
        }
    }
}
