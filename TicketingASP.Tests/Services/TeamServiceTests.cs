namespace TicketingASP.Tests.Services;

/// <summary>
/// Unit tests for TeamService
/// </summary>
public class TeamServiceTests
{
    private readonly Mock<ITeamRepository> _mockTeamRepository;
    private readonly Mock<ILogger<TeamService>> _mockLogger;
    private readonly TeamService _sut;

    public TeamServiceTests()
    {
        _mockTeamRepository = new Mock<ITeamRepository>();
        _mockLogger = new Mock<ILogger<TeamService>>();
        _sut = new TeamService(_mockTeamRepository.Object, _mockLogger.Object);
    }

    #region CreateTeamAsync Tests

    [Fact]
    public async Task CreateTeamAsync_WithValidDto_ReturnsTeamId()
    {
        // Arrange
        var createDto = new CreateTeamDto
        {
            Name = "Support Team",
            Description = "Customer support team"
        };
        var createdBy = 1;
        var expectedTeamId = 5;

        _mockTeamRepository
            .Setup(r => r.CreateAsync(createDto, createdBy))
            .ReturnsAsync(OperationResult<int>.SuccessResult(expectedTeamId));

        // Act
        var result = await _sut.CreateTeamAsync(createDto, createdBy);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(expectedTeamId);
    }

    [Fact]
    public async Task CreateTeamAsync_WhenDuplicateName_ReturnsFailure()
    {
        // Arrange
        var createDto = new CreateTeamDto { Name = "Existing Team" };
        var errorMessage = "Team with this name already exists";

        _mockTeamRepository
            .Setup(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), It.IsAny<int>()))
            .ReturnsAsync(OperationResult<int>.FailureResult(errorMessage));

        // Act
        var result = await _sut.CreateTeamAsync(createDto, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(errorMessage);
    }

    #endregion

    #region GetTeamByIdAsync Tests

    [Fact]
    public async Task GetTeamByIdAsync_WithValidId_ReturnsTeam()
    {
        // Arrange
        var teamId = 1;
        var expectedTeam = new TeamDto
        {
            Id = teamId,
            Name = "Support Team",
            Description = "Customer support"
        };

        _mockTeamRepository
            .Setup(r => r.GetByIdAsync(teamId))
            .ReturnsAsync(expectedTeam);

        // Act
        var result = await _sut.GetTeamByIdAsync(teamId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(teamId);
        result.Name.Should().Be("Support Team");
    }

    [Fact]
    public async Task GetTeamByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var teamId = 999;

        _mockTeamRepository
            .Setup(r => r.GetByIdAsync(teamId))
            .ReturnsAsync((TeamDto?)null);

        // Act
        var result = await _sut.GetTeamByIdAsync(teamId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateTeamAsync Tests

    [Fact]
    public async Task UpdateTeamAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var teamId = 1;
        var updateDto = new UpdateTeamDto
        {
            Name = "Updated Team Name",
            Description = "Updated description"
        };
        var updatedBy = 1;

        _mockTeamRepository
            .Setup(r => r.UpdateAsync(teamId, updateDto, updatedBy))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.UpdateTeamAsync(teamId, updateDto, updatedBy);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTeamAsync_WithNonExistentTeam_ReturnsFailure()
    {
        // Arrange
        var teamId = 999;
        var updateDto = new UpdateTeamDto { Name = "Test" };

        _mockTeamRepository
            .Setup(r => r.UpdateAsync(teamId, It.IsAny<UpdateTeamDto>(), It.IsAny<int>()))
            .ReturnsAsync(OperationResult.FailureResult("Team not found"));

        // Act
        var result = await _sut.UpdateTeamAsync(teamId, updateDto, 1);

        // Assert
        result.Success.Should().BeFalse();
    }

    #endregion

    #region DeleteTeamAsync Tests

    [Fact]
    public async Task DeleteTeamAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var teamId = 1;
        var deletedBy = 1;

        _mockTeamRepository
            .Setup(r => r.DeleteAsync(teamId, deletedBy))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.DeleteTeamAsync(teamId, deletedBy);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_ReturnsPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<TeamDto>
        {
            Items = new List<TeamDto>
            {
                new() { Id = 1, Name = "Team A" },
                new() { Id = 2, Name = "Team B" }
            },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 2
        };

        _mockTeamRepository
            .Setup(r => r.GetListAsync(1, 10, null, true))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.GetTeamsAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTeamsAsync_WithSearchAndActiveFilter_PassesToRepository()
    {
        // Arrange
        var search = "support";
        var isActive = false;

        _mockTeamRepository
            .Setup(r => r.GetListAsync(1, 10, search, isActive))
            .ReturnsAsync(new PagedResult<TeamDto>());

        // Act
        await _sut.GetTeamsAsync(1, 10, search, isActive);

        // Assert
        _mockTeamRepository.Verify(r => r.GetListAsync(1, 10, search, isActive), Times.Once);
    }

    #endregion

    #region AddMemberAsync Tests

    [Fact]
    public async Task AddMemberAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var teamId = 1;
        var memberDto = new AddTeamMemberDto
        {
            UserId = 5,
            Role = "Member"
        };
        var addedBy = 1;

        _mockTeamRepository
            .Setup(r => r.AddMemberAsync(teamId, memberDto.UserId, memberDto.Role, addedBy))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.AddMemberAsync(teamId, memberDto, addedBy);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AddMemberAsync_WhenUserAlreadyMember_ReturnsFailure()
    {
        // Arrange
        var teamId = 1;
        var memberDto = new AddTeamMemberDto { UserId = 5, Role = "Member" };

        _mockTeamRepository
            .Setup(r => r.AddMemberAsync(teamId, memberDto.UserId, memberDto.Role, It.IsAny<int>()))
            .ReturnsAsync(OperationResult.FailureResult("User is already a member of this team"));

        // Act
        var result = await _sut.AddMemberAsync(teamId, memberDto, 1);

        // Assert
        result.Success.Should().BeFalse();
    }

    #endregion

    #region RemoveMemberAsync Tests

    [Fact]
    public async Task RemoveMemberAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var teamId = 1;
        var userId = 5;

        _mockTeamRepository
            .Setup(r => r.RemoveMemberAsync(teamId, userId))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.RemoveMemberAsync(teamId, userId);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region GetMembersAsync Tests

    [Fact]
    public async Task GetMembersAsync_ReturnsTeamMembers()
    {
        // Arrange
        var teamId = 1;
        var expectedMembers = new List<TeamMemberDto>
        {
            new() { UserId = 1, UserName = "John Doe", Role = "Lead" },
            new() { UserId = 2, UserName = "Jane Smith", Role = "Member" }
        };

        _mockTeamRepository
            .Setup(r => r.GetMembersAsync(teamId))
            .ReturnsAsync(expectedMembers);

        // Act
        var result = await _sut.GetMembersAsync(teamId);

        // Assert
        result.Should().HaveCount(2);
        result[0].Role.Should().Be("Lead");
    }

    #endregion

    #region GetUserTeamsAsync Tests

    [Fact]
    public async Task GetUserTeamsAsync_ReturnsUserTeams()
    {
        // Arrange
        var userId = 1;
        var expectedTeams = new List<TeamDto>
        {
            new() { Id = 1, Name = "Team A" },
            new() { Id = 2, Name = "Team B" }
        };

        _mockTeamRepository
            .Setup(r => r.GetUserTeamsAsync(userId))
            .ReturnsAsync(expectedTeams);

        // Act
        var result = await _sut.GetUserTeamsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserTeamsAsync_WhenUserHasNoTeams_ReturnsEmptyList()
    {
        // Arrange
        var userId = 999;

        _mockTeamRepository
            .Setup(r => r.GetUserTeamsAsync(userId))
            .ReturnsAsync(new List<TeamDto>());

        // Act
        var result = await _sut.GetUserTeamsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
