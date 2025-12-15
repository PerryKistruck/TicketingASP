using Dapper;
using TicketingASP.Data;
using TicketingASP.Models.Common;
using TicketingASP.Models.DTOs;

namespace TicketingASP.Repositories;

/// <summary>
/// Repository interface for ticket operations using stored procedures
/// </summary>
public interface ITicketRepository
{
    Task<OperationResult<TicketDetailDto>> CreateAsync(CreateTicketDto dto, int requesterId, int? createdBy = null, string? ipAddress = null);
    Task<TicketDetailDto?> GetByIdAsync(int ticketId);
    Task<OperationResult> UpdateAsync(int ticketId, UpdateTicketDto dto, int updatedBy, string? ipAddress = null);
    Task<OperationResult> DeleteAsync(int ticketId, int deletedBy, string? ipAddress = null);
    Task<PagedResult<TicketListDto>> GetListAsync(TicketFilterDto filter, int userId, string userRole);
    Task<OperationResult<int>> AddCommentAsync(int ticketId, int userId, AddCommentDto dto, string? ipAddress = null);
    Task<List<TicketCommentDto>> GetCommentsAsync(int ticketId, bool includeInternal = false);
    Task<List<TicketHistoryDto>> GetHistoryAsync(int ticketId);
}

/// <summary>
/// Implementation of ticket repository using PostgreSQL stored procedures
/// </summary>
public class TicketRepository : ITicketRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(IDbConnectionFactory connectionFactory, ILogger<TicketRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<OperationResult<TicketDetailDto>> CreateAsync(CreateTicketDto dto, int requesterId, int? createdBy = null, string? ipAddress = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_ticket_create(@p_title, @p_description, @p_category_id, @p_priority_id, @p_requester_id, @p_assigned_to_id, @p_assigned_team_id, @p_due_date, @p_tags, @p_source, @p_created_by, @p_ip_address)",
                new
                {
                    p_title = dto.Title,
                    p_description = dto.Description,
                    p_category_id = dto.CategoryId,
                    p_priority_id = dto.PriorityId,
                    p_requester_id = requesterId,
                    p_assigned_to_id = dto.AssignedToId,
                    p_assigned_team_id = dto.AssignedTeamId,
                    p_due_date = dto.DueDate,
                    p_tags = dto.Tags,
                    p_source = "Web",
                    p_created_by = createdBy ?? requesterId,
                    p_ip_address = ipAddress
                });

            if (result == null || !result.success)
            {
                return OperationResult<TicketDetailDto>.FailureResult(result?.message ?? "Failed to create ticket");
            }

            var ticket = await GetByIdAsync((int)result.ticket_id);
            return OperationResult<TicketDetailDto>.SuccessResult(ticket!, "Ticket created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket");
            return OperationResult<TicketDetailDto>.FailureResult("An error occurred while creating the ticket");
        }
    }

    public async Task<TicketDetailDto?> GetByIdAsync(int ticketId)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var ticket = await connection.QueryFirstOrDefaultAsync<TicketDetailDto>(
                "SELECT * FROM sp_ticket_get_by_id(@p_ticket_id)",
                new { p_ticket_id = ticketId });

            if (ticket != null)
            {
                ticket.Comments = await GetCommentsAsync(ticketId, true);
                ticket.History = await GetHistoryAsync(ticketId);
            }

            return ticket;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket {TicketId}", ticketId);
            return null;
        }
    }

    public async Task<OperationResult> UpdateAsync(int ticketId, UpdateTicketDto dto, int updatedBy, string? ipAddress = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_ticket_update(@p_ticket_id, @p_title, @p_description, @p_category_id, @p_priority_id, @p_status_id, @p_assigned_to_id, @p_assigned_team_id, @p_due_date, @p_tags, @p_updated_by, @p_ip_address)",
                new
                {
                    p_ticket_id = ticketId,
                    p_title = dto.Title,
                    p_description = dto.Description,
                    p_category_id = dto.CategoryId,
                    p_priority_id = dto.PriorityId,
                    p_status_id = dto.StatusId,
                    p_assigned_to_id = dto.AssignedToId,
                    p_assigned_team_id = dto.AssignedTeamId,
                    p_due_date = dto.DueDate,
                    p_tags = dto.Tags,
                    p_updated_by = updatedBy,
                    p_ip_address = ipAddress
                });

            return result?.success == true 
                ? OperationResult.SuccessResult(result.message) 
                : OperationResult.FailureResult(result?.message ?? "Failed to update ticket");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket {TicketId}", ticketId);
            return OperationResult.FailureResult("An error occurred while updating the ticket");
        }
    }

    public async Task<OperationResult> DeleteAsync(int ticketId, int deletedBy, string? ipAddress = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_ticket_delete(@p_ticket_id, @p_deleted_by, @p_ip_address)",
                new
                {
                    p_ticket_id = ticketId,
                    p_deleted_by = deletedBy,
                    p_ip_address = ipAddress
                });

            return result?.success == true 
                ? OperationResult.SuccessResult(result.message) 
                : OperationResult.FailureResult(result?.message ?? "Failed to delete ticket");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ticket {TicketId}", ticketId);
            return OperationResult.FailureResult("An error occurred while deleting the ticket");
        }
    }

    public async Task<PagedResult<TicketListDto>> GetListAsync(TicketFilterDto filter, int userId, string userRole)
    {
        try
        {
            _logger.LogDebug("Getting ticket list for user {UserId} with role {UserRole}", userId, userRole);
            
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var tickets = await connection.QueryAsync<TicketListDto>(
                @"SELECT id, ticket_number AS TicketNumber, title, priority_name AS PriorityName, priority_color AS PriorityColour,
                         status_name AS StatusName, status_color AS StatusColour, requester_name AS RequesterName,
                         assigned_to_name AS AssignedToName, team_name AS TeamName, created_at AS CreatedAt, updated_at AS UpdatedAt,
                         total_count AS TotalCount
                  FROM sp_ticket_list(@p_page_number, @p_page_size, @p_search, @p_status_id, @p_priority_id, @p_category_id,
                                      @p_assigned_to_id, @p_assigned_team_id, @p_requester_id, @p_date_from, @p_date_to,
                                      @p_include_closed, @p_user_id, @p_user_role)",
                new
                {
                    p_page_number = filter.PageNumber,
                    p_page_size = filter.PageSize,
                    p_search = filter.Search,
                    p_status_id = filter.StatusId,
                    p_priority_id = filter.PriorityId,
                    p_category_id = filter.CategoryId,
                    p_assigned_to_id = filter.AssignedToId,
                    p_assigned_team_id = filter.AssignedTeamId,
                    p_requester_id = filter.RequesterId,
                    p_date_from = filter.DateFrom,
                    p_date_to = filter.DateTo,
                    p_include_closed = filter.IncludeClosed,
                    p_user_id = userId,
                    p_user_role = userRole
                });

            var ticketList = tickets.ToList();
            _logger.LogDebug("Query returned {Count} tickets", ticketList.Count);
            
            // TotalCount is included in each row from the stored procedure
            var totalCount = ticketList.FirstOrDefault()?.TotalCount ?? 0;

            return new PagedResult<TicketListDto>
            {
                Items = ticketList,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket list");
            return new PagedResult<TicketListDto>();
        }
    }

    public async Task<OperationResult<int>> AddCommentAsync(int ticketId, int userId, AddCommentDto dto, string? ipAddress = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_ticket_add_comment(@p_ticket_id, @p_user_id, @p_content, @p_is_internal, @p_is_resolution, @p_ip_address)",
                new
                {
                    p_ticket_id = ticketId,
                    p_user_id = userId,
                    p_content = dto.Content,
                    p_is_internal = dto.IsInternal,
                    p_is_resolution = dto.IsResolution,
                    p_ip_address = ipAddress
                });

            return result?.success == true 
                ? OperationResult<int>.SuccessResult((int)result.comment_id, result.message) 
                : OperationResult<int>.FailureResult(result?.message ?? "Failed to add comment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to ticket {TicketId}", ticketId);
            return OperationResult<int>.FailureResult("An error occurred while adding the comment");
        }
    }

    public async Task<List<TicketCommentDto>> GetCommentsAsync(int ticketId, bool includeInternal = false)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var comments = await connection.QueryAsync<TicketCommentDto>(
                @"SELECT id, user_id AS UserId, user_name AS UserName, user_avatar AS UserAvatar, content,
                         is_internal AS IsInternal, is_resolution AS IsResolution, created_at AS CreatedAt, updated_at AS UpdatedAt
                  FROM sp_ticket_get_comments(@p_ticket_id, @p_include_internal)",
                new { p_ticket_id = ticketId, p_include_internal = includeInternal });

            return comments.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for ticket {TicketId}", ticketId);
            return new List<TicketCommentDto>();
        }
    }

    public async Task<List<TicketHistoryDto>> GetHistoryAsync(int ticketId)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var history = await connection.QueryAsync<TicketHistoryDto>(
                @"SELECT id, user_id AS UserId, user_name AS ChangedByName, action, field_name AS FieldName,
                         old_value AS OldValue, new_value AS NewValue, description, created_at AS ChangedAt
                  FROM sp_ticket_get_history(@p_ticket_id)",
                new { p_ticket_id = ticketId });

            return history.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting history for ticket {TicketId}", ticketId);
            return new List<TicketHistoryDto>();
        }
    }
}
