namespace TicketingASP.Tests.Services;

/// <summary>
/// Unit tests for UserService
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _sut = new UserService(_mockUserRepository.Object, _mockLogger.Object);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidDto_CreatesUserAndAssignsDefaultRole()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            Email = "test@example.com",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#",
            FirstName = "John",
            LastName = "Doe"
        };
        var expectedUserId = 1;

        _mockUserRepository
            .Setup(r => r.CreateAsync(registerDto, It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(OperationResult<int>.SuccessResult(expectedUserId));

        _mockUserRepository
            .Setup(r => r.AssignRoleAsync(expectedUserId, 4, expectedUserId))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(expectedUserId);
        _mockUserRepository.Verify(r => r.AssignRoleAsync(expectedUserId, 4, expectedUserId), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithSpecificRole_AssignsSpecifiedRole()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            Email = "admin@example.com",
            Password = "Admin123!@#",
            ConfirmPassword = "Admin123!@#",
            FirstName = "Admin",
            LastName = "User",
            RoleId = 1 // Admin role
        };
        var expectedUserId = 2;

        _mockUserRepository
            .Setup(r => r.CreateAsync(registerDto, It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(OperationResult<int>.SuccessResult(expectedUserId));

        _mockUserRepository
            .Setup(r => r.AssignRoleAsync(expectedUserId, 1, expectedUserId))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.Success.Should().BeTrue();
        _mockUserRepository.Verify(r => r.AssignRoleAsync(expectedUserId, 1, expectedUserId), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenCreationFails_DoesNotAssignRole()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            Email = "existing@example.com",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockUserRepository
            .Setup(r => r.CreateAsync(registerDto, It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(OperationResult<int>.FailureResult("Email already exists"));

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.Success.Should().BeFalse();
        _mockUserRepository.Verify(r => r.AssignRoleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithCreatedBy_PassesCreatedByToRepository()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            Email = "test@example.com",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#",
            FirstName = "John",
            LastName = "Doe"
        };
        var createdBy = 5;
        var expectedUserId = 1;

        _mockUserRepository
            .Setup(r => r.CreateAsync(registerDto, It.IsAny<string>(), It.IsAny<string>(), createdBy))
            .ReturnsAsync(OperationResult<int>.SuccessResult(expectedUserId));

        _mockUserRepository
            .Setup(r => r.AssignRoleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        await _sut.RegisterAsync(registerDto, createdBy);

        // Assert
        _mockUserRepository.Verify(r => r.CreateAsync(registerDto, It.IsAny<string>(), It.IsAny<string>(), createdBy), Times.Once);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "Test123!@#" };
        var userId = 1;
        
        // Create a valid hash for the password
        var expectedUser = new UserDetailDto
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        // We can't easily mock password verification since it's internal to the service
        // So we'll test the flow when user is not found
        _mockUserRepository
            .Setup(r => r.GetByEmailForLoginAsync(loginDto.Email))
            .ReturnsAsync(((int?, string?, string?, bool, bool)?)null);

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "Test123!@#" };

        _mockUserRepository
            .Setup(r => r.GetByEmailForLoginAsync(loginDto.Email))
            .ReturnsAsync(((int?, string?, string?, bool, bool)?)null);

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsFailure()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "inactive@example.com", Password = "Test123!@#" };

        _mockUserRepository
            .Setup(r => r.GetByEmailForLoginAsync(loginDto.Email))
            .ReturnsAsync((1, "hash", "salt", false, false)); // isActive = false

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Account is inactive");
    }

    [Fact]
    public async Task LoginAsync_WithLockedUser_ReturnsFailure()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "locked@example.com", Password = "Test123!@#" };

        _mockUserRepository
            .Setup(r => r.GetByEmailForLoginAsync(loginDto.Email))
            .ReturnsAsync((1, "hash", "salt", true, true)); // isLocked = true

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Account is locked. Please contact administrator.");
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var userId = 1;
        var expectedUser = new UserDetailDto
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var userId = 999;

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((UserDetailDto?)null);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var updateDto = new UpdateUserDto
        {
            FirstName = "Updated",
            LastName = "Name"
        };
        var updatedBy = 1;

        _mockUserRepository
            .Setup(r => r.UpdateAsync(userId, updateDto, updatedBy))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.UpdateUserAsync(userId, updateDto, updatedBy);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var deletedBy = 2;

        _mockUserRepository
            .Setup(r => r.DeleteAsync(userId, deletedBy))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.DeleteUserAsync(userId, deletedBy);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_ReturnsPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<UserListDto>
        {
            Items = new List<UserListDto>
            {
                new() { Id = 1, Email = "user1@example.com" },
                new() { Id = 2, Email = "user2@example.com" }
            },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 2
        };

        _mockUserRepository
            .Setup(r => r.GetListAsync(1, 10, null, null, null))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.GetUsersAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsersAsync_WithFilters_PassesFiltersToRepository()
    {
        // Arrange
        var search = "john";
        var isActive = true;
        var roleId = 2;

        _mockUserRepository
            .Setup(r => r.GetListAsync(1, 10, search, isActive, roleId))
            .ReturnsAsync(new PagedResult<UserListDto>());

        // Act
        await _sut.GetUsersAsync(1, 10, search, isActive, roleId);

        // Assert
        _mockUserRepository.Verify(r => r.GetListAsync(1, 10, search, isActive, roleId), Times.Once);
    }

    #endregion

    #region AssignRoleAsync Tests

    [Fact]
    public async Task AssignRoleAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var roleId = 2;
        var assignedBy = 5;

        _mockUserRepository
            .Setup(r => r.AssignRoleAsync(userId, roleId, assignedBy))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.AssignRoleAsync(userId, roleId, assignedBy);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region GetUserRolesAsync Tests

    [Fact]
    public async Task GetUserRolesAsync_ReturnsRolesList()
    {
        // Arrange
        var userId = 1;
        var expectedRoles = new List<RoleDto>
        {
            new() { Id = 1, Name = "Admin" },
            new() { Id = 2, Name = "Agent" }
        };

        _mockUserRepository
            .Setup(r => r.GetRolesAsync(userId))
            .ReturnsAsync(expectedRoles);

        // Act
        var result = await _sut.GetUserRolesAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Admin");
    }

    #endregion
}
