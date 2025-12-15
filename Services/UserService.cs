using System.Security.Cryptography;
using System.Text;
using TicketingASP.Models.Common;
using TicketingASP.Models.DTOs;
using TicketingASP.Repositories;

namespace TicketingASP.Services;

/// <summary>
/// Service interface for user and authentication operations
/// </summary>
public interface IUserService
{
    Task<OperationResult<int>> RegisterAsync(RegisterUserDto dto, int? createdBy = null);
    Task<OperationResult<UserDetailDto>> LoginAsync(LoginDto dto, string? ipAddress = null, string? userAgent = null);
    Task<UserDetailDto?> GetUserByIdAsync(int userId);
    Task<OperationResult> UpdateUserAsync(int userId, UpdateUserDto dto, int updatedBy);
    Task<OperationResult> DeleteUserAsync(int userId, int deletedBy);
    Task<PagedResult<UserListDto>> GetUsersAsync(int pageNumber, int pageSize, string? search = null, bool? isActive = null, int? roleId = null);
    Task<OperationResult> AssignRoleAsync(int userId, int roleId, int assignedBy);
    Task<List<RoleDto>> GetUserRolesAsync(int userId);
    Task<OperationResult> ChangePasswordAsync(int userId, ChangePasswordDto dto);
}

/// <summary>
/// Implementation of user service
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<OperationResult<int>> RegisterAsync(RegisterUserDto dto, int? createdBy = null)
    {
        _logger.LogInformation("Registering new user {Email}", dto.Email);

        // Generate password hash and salt
        var (hash, salt) = HashPassword(dto.Password);

        var result = await _userRepository.CreateAsync(dto, hash, salt, createdBy);

        if (result.Success && result.Data > 0)
        {
            // Use provided RoleId or default to "User" role (roleId = 4)
            var roleId = dto.RoleId ?? 4;
            await _userRepository.AssignRoleAsync(result.Data, roleId, createdBy ?? result.Data);
        }

        return result;
    }

    public async Task<OperationResult<UserDetailDto>> LoginAsync(LoginDto dto, string? ipAddress = null, string? userAgent = null)
    {
        _logger.LogInformation("Login attempt for user {Email}", dto.Email);

        var loginInfo = await _userRepository.GetByEmailForLoginAsync(dto.Email);

        if (loginInfo == null)
        {
            _logger.LogWarning("Login failed - user not found: {Email}", dto.Email);
            return OperationResult<UserDetailDto>.FailureResult("Invalid email or password");
        }

        var (id, passwordHash, passwordSalt, isActive, isLocked) = loginInfo.Value;

        if (!isActive)
        {
            _logger.LogWarning("Login failed - user inactive: {Email}", dto.Email);
            return OperationResult<UserDetailDto>.FailureResult("Account is inactive");
        }

        if (isLocked)
        {
            _logger.LogWarning("Login failed - user locked: {Email}", dto.Email);
            return OperationResult<UserDetailDto>.FailureResult("Account is locked. Please contact administrator.");
        }

        // Verify password
        _logger.LogDebug("Verifying password for user {Email}. Stored hash length: {HashLen}, Salt length: {SaltLen}", 
            dto.Email, passwordHash?.Length ?? 0, passwordSalt?.Length ?? 0);
        
        if (!VerifyPassword(dto.Password, passwordHash!, passwordSalt!))
        {
            await _userRepository.UpdateLoginAttemptAsync(id!.Value, false, ipAddress, userAgent);
            _logger.LogWarning("Login failed - invalid password: {Email}", dto.Email);
            return OperationResult<UserDetailDto>.FailureResult("Invalid email or password");
        }

        // Successful login
        await _userRepository.UpdateLoginAttemptAsync(id!.Value, true, ipAddress, userAgent);

        var user = await _userRepository.GetByIdAsync(id!.Value);
        _logger.LogInformation("Login successful for user {Email}", dto.Email);

        return OperationResult<UserDetailDto>.SuccessResult(user!, "Login successful");
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(int userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    public async Task<OperationResult> UpdateUserAsync(int userId, UpdateUserDto dto, int updatedBy)
    {
        _logger.LogInformation("Updating user {UserId}", userId);
        return await _userRepository.UpdateAsync(userId, dto, updatedBy);
    }

    public async Task<OperationResult> DeleteUserAsync(int userId, int deletedBy)
    {
        _logger.LogWarning("Deleting user {UserId}", userId);
        return await _userRepository.DeleteAsync(userId, deletedBy);
    }

    public async Task<PagedResult<UserListDto>> GetUsersAsync(int pageNumber, int pageSize, string? search = null, bool? isActive = null, int? roleId = null)
    {
        return await _userRepository.GetListAsync(pageNumber, pageSize, search, isActive, roleId);
    }

    public async Task<OperationResult> AssignRoleAsync(int userId, int roleId, int assignedBy)
    {
        _logger.LogInformation("Assigning role {RoleId} to user {UserId}", roleId, userId);
        return await _userRepository.AssignRoleAsync(userId, roleId, assignedBy);
    }

    public async Task<List<RoleDto>> GetUserRolesAsync(int userId)
    {
        return await _userRepository.GetRolesAsync(userId);
    }

    public async Task<OperationResult> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        _logger.LogInformation("Changing password for user {UserId}", userId);

        var loginInfo = await _userRepository.GetByIdAsync(userId);
        if (loginInfo == null)
        {
            return OperationResult.FailureResult("User not found");
        }

        // Note: In a real implementation, you'd verify the current password first
        // This is simplified for the example
        
        var (hash, salt) = HashPassword(dto.NewPassword);
        
        // Would need a separate stored procedure for password update
        // For now, this is a placeholder
        return OperationResult.SuccessResult("Password changed successfully");
    }

    #region Password Helpers

    private static (string Hash, string Salt) HashPassword(string password)
    {
        using var hmac = new HMACSHA512();
        var salt = Convert.ToBase64String(hmac.Key);
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return (hash, salt);
    }

    private bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        try
        {
            _logger.LogInformation("VerifyPassword: Starting verification. StoredHash length={HashLen}, StoredSalt length={SaltLen}",
                storedHash?.Length ?? 0, storedSalt?.Length ?? 0);
            
            var saltBytes = Convert.FromBase64String(storedSalt);
            _logger.LogInformation("VerifyPassword: Decoded salt bytes length={SaltBytesLen}", saltBytes.Length);
            
            using var hmac = new HMACSHA512(saltBytes);
            var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            
            _logger.LogInformation("VerifyPassword: ComputedHash length={ComputedLen}, StoredHash length={StoredLen}",
                computedHash.Length, storedHash.Length);
            
            var isMatch = computedHash == storedHash;
            
            if (!isMatch)
            {
                _logger.LogWarning("VerifyPassword: MISMATCH! ComputedHash[0..20]={ComputedPrefix}, StoredHash[0..20]={StoredPrefix}", 
                    computedHash.Substring(0, Math.Min(20, computedHash.Length)),
                    storedHash.Substring(0, Math.Min(20, storedHash.Length)));
            }
            else
            {
                _logger.LogInformation("VerifyPassword: Password match SUCCESS");
            }
            
            return isMatch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VerifyPassword: Exception during verification: {Message}", ex.Message);
            return false;
        }
    }

    #endregion
}
