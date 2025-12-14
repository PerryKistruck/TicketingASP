using TicketingASP.Models.Common;
using TicketingASP.Models.DTOs;
using TicketingASP.Repositories;

namespace TicketingASP.Services;

/// <summary>
/// Service interface for team operations
/// </summary>
public interface ITeamService
{
    Task<OperationResult<int>> CreateTeamAsync(CreateTeamDto dto, int createdBy);
    Task<TeamDto?> GetTeamByIdAsync(int teamId);
    Task<OperationResult> UpdateTeamAsync(int teamId, UpdateTeamDto dto, int updatedBy);
    Task<OperationResult> DeleteTeamAsync(int teamId, int deletedBy);
    Task<PagedResult<TeamDto>> GetTeamsAsync(int pageNumber, int pageSize, string? search = null, bool? isActive = true);
    Task<OperationResult> AddMemberAsync(int teamId, AddTeamMemberDto dto, int addedBy);
    Task<OperationResult> RemoveMemberAsync(int teamId, int userId);
    Task<List<TeamMemberDto>> GetMembersAsync(int teamId);
    Task<List<TeamDto>> GetUserTeamsAsync(int userId);
}

/// <summary>
/// Implementation of team service
/// </summary>
public class TeamService : ITeamService
{
    private readonly ITeamRepository _teamRepository;
    private readonly ILogger<TeamService> _logger;

    public TeamService(ITeamRepository teamRepository, ILogger<TeamService> logger)
    {
        _teamRepository = teamRepository;
        _logger = logger;
    }

    public async Task<OperationResult<int>> CreateTeamAsync(CreateTeamDto dto, int createdBy)
    {
        _logger.LogInformation("Creating team {TeamName}", dto.Name);
        return await _teamRepository.CreateAsync(dto, createdBy);
    }

    public async Task<TeamDto?> GetTeamByIdAsync(int teamId)
    {
        return await _teamRepository.GetByIdAsync(teamId);
    }

    public async Task<OperationResult> UpdateTeamAsync(int teamId, UpdateTeamDto dto, int updatedBy)
    {
        _logger.LogInformation("Updating team {TeamId}", teamId);
        return await _teamRepository.UpdateAsync(teamId, dto, updatedBy);
    }

    public async Task<OperationResult> DeleteTeamAsync(int teamId, int deletedBy)
    {
        _logger.LogWarning("Deleting team {TeamId}", teamId);
        return await _teamRepository.DeleteAsync(teamId, deletedBy);
    }

    public async Task<PagedResult<TeamDto>> GetTeamsAsync(int pageNumber, int pageSize, string? search = null, bool? isActive = true)
    {
        return await _teamRepository.GetListAsync(pageNumber, pageSize, search, isActive);
    }

    public async Task<OperationResult> AddMemberAsync(int teamId, AddTeamMemberDto dto, int addedBy)
    {
        _logger.LogInformation("Adding member {UserId} to team {TeamId}", dto.UserId, teamId);
        return await _teamRepository.AddMemberAsync(teamId, dto.UserId, dto.Role, addedBy);
    }

    public async Task<OperationResult> RemoveMemberAsync(int teamId, int userId)
    {
        _logger.LogInformation("Removing member {UserId} from team {TeamId}", userId, teamId);
        return await _teamRepository.RemoveMemberAsync(teamId, userId);
    }

    public async Task<List<TeamMemberDto>> GetMembersAsync(int teamId)
    {
        return await _teamRepository.GetMembersAsync(teamId);
    }

    public async Task<List<TeamDto>> GetUserTeamsAsync(int userId)
    {
        return await _teamRepository.GetUserTeamsAsync(userId);
    }
}
