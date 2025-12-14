using TicketingASP.Models.Common;
using TicketingASP.Models.DTOs;
using TicketingASP.Repositories;

namespace TicketingASP.Services;

/// <summary>
/// Service interface for ticket business logic
/// </summary>
public interface ITicketService
{
    Task<OperationResult<TicketDetailDto>> CreateTicketAsync(CreateTicketDto dto, int requesterId, int? createdBy = null, string? ipAddress = null);
    Task<TicketDetailDto?> GetTicketByIdAsync(int ticketId);
    Task<OperationResult> UpdateTicketAsync(int ticketId, UpdateTicketDto dto, int updatedBy, string? ipAddress = null);
    Task<OperationResult> DeleteTicketAsync(int ticketId, int deletedBy, string? ipAddress = null);
    Task<PagedResult<TicketListDto>> GetTicketsAsync(TicketFilterDto filter, int userId, string userRole);
    Task<OperationResult<int>> AddCommentAsync(int ticketId, int userId, AddCommentDto dto, string? ipAddress = null);
    Task<List<TicketCommentDto>> GetCommentsAsync(int ticketId, bool includeInternal = false);
    Task<List<TicketHistoryDto>> GetHistoryAsync(int ticketId);
    Task<OperationResult> AssignTicketAsync(int ticketId, int? assignedToId, int? assignedTeamId, int updatedBy, string? ipAddress = null);
    Task<OperationResult> ChangeStatusAsync(int ticketId, int statusId, int updatedBy, string? ipAddress = null);
}

/// <summary>
/// Implementation of ticket service
/// </summary>
public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<TicketService> _logger;

    public TicketService(ITicketRepository ticketRepository, ILogger<TicketService> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public async Task<OperationResult<TicketDetailDto>> CreateTicketAsync(CreateTicketDto dto, int requesterId, int? createdBy = null, string? ipAddress = null)
    {
        _logger.LogInformation("Creating ticket for requester {RequesterId}", requesterId);
        return await _ticketRepository.CreateAsync(dto, requesterId, createdBy, ipAddress);
    }

    public async Task<TicketDetailDto?> GetTicketByIdAsync(int ticketId)
    {
        return await _ticketRepository.GetByIdAsync(ticketId);
    }

    public async Task<OperationResult> UpdateTicketAsync(int ticketId, UpdateTicketDto dto, int updatedBy, string? ipAddress = null)
    {
        _logger.LogInformation("Updating ticket {TicketId} by user {UserId}", ticketId, updatedBy);
        return await _ticketRepository.UpdateAsync(ticketId, dto, updatedBy, ipAddress);
    }

    public async Task<OperationResult> DeleteTicketAsync(int ticketId, int deletedBy, string? ipAddress = null)
    {
        _logger.LogWarning("Deleting ticket {TicketId} by user {UserId}", ticketId, deletedBy);
        return await _ticketRepository.DeleteAsync(ticketId, deletedBy, ipAddress);
    }

    public async Task<PagedResult<TicketListDto>> GetTicketsAsync(TicketFilterDto filter, int userId, string userRole)
    {
        return await _ticketRepository.GetListAsync(filter, userId, userRole);
    }

    public async Task<OperationResult<int>> AddCommentAsync(int ticketId, int userId, AddCommentDto dto, string? ipAddress = null)
    {
        _logger.LogInformation("Adding comment to ticket {TicketId} by user {UserId}", ticketId, userId);
        return await _ticketRepository.AddCommentAsync(ticketId, userId, dto, ipAddress);
    }

    public async Task<List<TicketCommentDto>> GetCommentsAsync(int ticketId, bool includeInternal = false)
    {
        return await _ticketRepository.GetCommentsAsync(ticketId, includeInternal);
    }

    public async Task<List<TicketHistoryDto>> GetHistoryAsync(int ticketId)
    {
        return await _ticketRepository.GetHistoryAsync(ticketId);
    }

    public async Task<OperationResult> AssignTicketAsync(int ticketId, int? assignedToId, int? assignedTeamId, int updatedBy, string? ipAddress = null)
    {
        _logger.LogInformation("Assigning ticket {TicketId} to user {AssignedToId} / team {TeamId}", ticketId, assignedToId, assignedTeamId);
        
        var dto = new UpdateTicketDto
        {
            AssignedToId = assignedToId,
            AssignedTeamId = assignedTeamId
        };
        
        return await _ticketRepository.UpdateAsync(ticketId, dto, updatedBy, ipAddress);
    }

    public async Task<OperationResult> ChangeStatusAsync(int ticketId, int statusId, int updatedBy, string? ipAddress = null)
    {
        _logger.LogInformation("Changing status of ticket {TicketId} to {StatusId}", ticketId, statusId);
        
        var dto = new UpdateTicketDto { StatusId = statusId };
        return await _ticketRepository.UpdateAsync(ticketId, dto, updatedBy, ipAddress);
    }
}
