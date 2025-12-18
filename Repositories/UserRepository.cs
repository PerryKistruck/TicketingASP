using Dapper;
using TicketingASP.Data;
using TicketingASP.Models.Common;
using TicketingASP.Models.DTOs;

namespace TicketingASP.Repositories;

/// <summary>
/// Repository interface for user operations using stored procedures
/// </summary>
public interface IUserRepository
{
    Task<OperationResult<int>> CreateAsync(RegisterUserDto dto, string passwordHash, string passwordSalt, int? createdBy = null);
    Task<UserDetailDto?> GetByIdAsync(int userId);
    Task<(int? Id, string? PasswordHash, string? PasswordSalt, bool IsActive, bool IsLocked)?> GetByEmailForLoginAsync(string email);
    Task<OperationResult> UpdateAsync(int userId, UpdateUserDto dto, int updatedBy);
    Task<OperationResult> DeleteAsync(int userId, int deletedBy);
    Task<PagedResult<UserListDto>> GetListAsync(int pageNumber, int pageSize, string? search = null, bool? isActive = null, int? roleId = null);
    Task<OperationResult> AssignRoleAsync(int userId, int roleId, int assignedBy);
    Task<List<RoleDto>> GetRolesAsync(int userId);
    Task UpdateLoginAttemptAsync(int userId, bool success, string? ipAddress = null, string? userAgent = null);
    Task<OperationResult> UnlockUserAsync(int userId, int unlockedBy);
    Task<OperationResult> ResetPasswordAsync(int userId, string passwordHash, string passwordSalt, int resetBy);
}

/// <summary>
/// Implementation of user repository using PostgreSQL stored procedures
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(IDbConnectionFactory connectionFactory, ILogger<UserRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<OperationResult<int>> CreateAsync(RegisterUserDto dto, string passwordHash, string passwordSalt, int? createdBy = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_user_create(@p_email, @p_password_hash, @p_password_salt, @p_first_name, @p_last_name, @p_phone, @p_created_by)",
                new
                {
                    p_email = dto.Email.ToLower(),
                    p_password_hash = passwordHash,
                    p_password_salt = passwordSalt,
                    p_first_name = dto.FirstName,
                    p_last_name = dto.LastName,
                    p_phone = dto.Phone,
                    p_created_by = createdBy
                });

            return result?.success == true 
                ? OperationResult<int>.SuccessResult((int)result.user_id, result.message) 
                : OperationResult<int>.FailureResult(result?.message ?? "Failed to create user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email}: {Message}", dto.Email, ex.Message);
            return OperationResult<int>.FailureResult($"Database error: {ex.Message}");
        }
    }

    public async Task<UserDetailDto?> GetByIdAsync(int userId)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var user = await connection.QueryFirstOrDefaultAsync<UserDetailDto>(
                @"SELECT id, email, first_name AS FirstName, last_name AS LastName, display_name AS DisplayName,
                         phone, avatar_url AS AvatarUrl, is_active AS IsActive, is_locked AS IsLocked,
                         last_login_at AS LastLoginAt, created_at AS CreatedAt
                  FROM sp_user_get_by_id(@p_user_id)",
                new { p_user_id = userId });

            if (user != null)
            {
                user.Roles = await GetRolesAsync(userId);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<(int? Id, string? PasswordHash, string? PasswordSalt, bool IsActive, bool IsLocked)?> GetByEmailForLoginAsync(string email)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();

            _logger.LogDebug("GetByEmailForLoginAsync: Attempting to find user by email");

            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_user_get_by_email(@p_email)",
                new { p_email = email.ToLower() });

            if (result == null)
            {
                _logger.LogDebug("GetByEmailForLoginAsync: No user found with the provided email");
                return null;
            }

            // Cast dynamic values to static types for logging
            int userId = (int)result.id;
            string passwordHash = (string)result.password_hash;
            string passwordSalt = (string)result.password_salt;
            bool isActive = (bool)result.is_active;
            bool isLocked = (bool)result.is_locked;

            _logger.LogInformation("GetByEmailForLoginAsync: Found user id={UserId}, hash_length={HashLen}, salt_length={SaltLen}, is_active={IsActive}, is_locked={IsLocked}",
                userId, passwordHash?.Length ?? 0, passwordSalt?.Length ?? 0, isActive, isLocked);

            return (userId, passwordHash, passwordSalt, isActive, isLocked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByEmailForLoginAsync for email");
            return null;
        }
    }

    public async Task<OperationResult> UpdateAsync(int userId, UpdateUserDto dto, int updatedBy)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_user_update(@p_user_id, @p_first_name, @p_last_name, @p_phone, @p_avatar_url, @p_updated_by)",
                new
                {
                    p_user_id = userId,
                    p_first_name = dto.FirstName,
                    p_last_name = dto.LastName,
                    p_phone = dto.Phone,
                    p_avatar_url = dto.AvatarUrl,
                    p_updated_by = updatedBy
                });

            return result?.success == true 
                ? OperationResult.SuccessResult(result.message) 
                : OperationResult.FailureResult(result?.message ?? "Failed to update user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return OperationResult.FailureResult("An error occurred while updating the user");
        }
    }

    public async Task<OperationResult> DeleteAsync(int userId, int deletedBy)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_user_delete(@p_user_id, @p_deleted_by)",
                new { p_user_id = userId, p_deleted_by = deletedBy });

            return result?.success == true 
                ? OperationResult.SuccessResult(result.message) 
                : OperationResult.FailureResult(result?.message ?? "Failed to delete user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return OperationResult.FailureResult("An error occurred while deleting the user");
        }
    }

    public async Task<PagedResult<UserListDto>> GetListAsync(int pageNumber, int pageSize, string? search = null, bool? isActive = null, int? roleId = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var users = await connection.QueryAsync<UserListDto>(
                @"SELECT id, email, first_name AS FirstName, last_name AS LastName, display_name AS DisplayName,
                         is_active AS IsActive, is_locked AS IsLocked, created_at AS CreatedAt, roles, total_count AS TotalCount
                  FROM sp_user_list(@p_page_number, @p_page_size, @p_search, @p_is_active, @p_role_id)",
                new
                {
                    p_page_number = pageNumber,
                    p_page_size = pageSize,
                    p_search = search,
                    p_is_active = isActive,
                    p_role_id = roleId
                });

            var userList = users.ToList();
            var totalCount = userList.FirstOrDefault()?.TotalCount ?? 0;

            return new PagedResult<UserListDto>
            {
                Items = userList,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user list");
            return new PagedResult<UserListDto>();
        }
    }

    public async Task<OperationResult> AssignRoleAsync(int userId, int roleId, int assignedBy)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_user_assign_role(@p_user_id, @p_role_id, @p_assigned_by)",
                new { p_user_id = userId, p_role_id = roleId, p_assigned_by = assignedBy });

            return result?.success == true 
                ? OperationResult.SuccessResult(result.message) 
                : OperationResult.FailureResult(result?.message ?? "Failed to assign role");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
            return OperationResult.FailureResult("An error occurred while assigning the role");
        }
    }

    public async Task<List<RoleDto>> GetRolesAsync(int userId)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            var roles = await connection.QueryAsync<RoleDto>(
                @"SELECT role_id AS Id, role_name AS Name, permissions
                  FROM sp_user_get_roles(@p_user_id)",
                new { p_user_id = userId });

            return roles.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            return new List<RoleDto>();
        }
    }

    public async Task UpdateLoginAttemptAsync(int userId, bool success, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            await connection.ExecuteAsync(
                "SELECT sp_user_update_login_attempt(@p_user_id, @p_success, @p_ip_address, @p_user_agent)",
                new { p_user_id = userId, p_success = success, p_ip_address = ipAddress, p_user_agent = userAgent });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating login attempt for user {UserId}", userId);
        }
    }

    public async Task<OperationResult> UnlockUserAsync(int userId, int unlockedBy)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            // Reset failed login attempts and unlock the user
            await connection.ExecuteAsync(
                @"UPDATE users 
                  SET islocked = FALSE, 
                      failedloginattempts = 0,
                      updatedat = NOW()
                  WHERE id = @userId",
                new { userId });
            
            _logger.LogInformation("User {UserId} unlocked by {UnlockedBy}", userId, unlockedBy);
            return OperationResult.SuccessResult("User account unlocked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user {UserId}", userId);
            return OperationResult.FailureResult("An error occurred while unlocking the user account");
        }
    }

    public async Task<OperationResult> ResetPasswordAsync(int userId, string passwordHash, string passwordSalt, int resetBy)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            // Update password hash and salt
            await connection.ExecuteAsync(
                @"UPDATE users 
                  SET passwordhash = @passwordHash, 
                      passwordsalt = @passwordSalt,
                      islocked = FALSE,
                      failedloginattempts = 0,
                      updatedat = NOW()
                  WHERE id = @userId",
                new { userId, passwordHash, passwordSalt });
            
            _logger.LogInformation("Password reset for user {UserId} by {ResetBy}", userId, resetBy);
            return OperationResult.SuccessResult("Password reset successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            return OperationResult.FailureResult("An error occurred while resetting the password");
        }
    }
}
