namespace TicketingASP.Tests.Services;

/// <summary>
/// Unit tests for LookupService
/// </summary>
public class LookupServiceTests
{
    private readonly Mock<ILookupRepository> _mockLookupRepository;
    private readonly Mock<ILogger<LookupService>> _mockLogger;
    private readonly LookupService _sut;

    public LookupServiceTests()
    {
        _mockLookupRepository = new Mock<ILookupRepository>();
        _mockLogger = new Mock<ILogger<LookupService>>();
        _sut = new LookupService(_mockLookupRepository.Object, _mockLogger.Object);
    }

    #region GetPrioritiesAsync Tests

    [Fact]
    public async Task GetPrioritiesAsync_ReturnsPrioritiesList()
    {
        // Arrange
        var expectedPriorities = new List<LookupItemDto>
        {
            new() { Id = 1, Name = "Low" },
            new() { Id = 2, Name = "Medium" },
            new() { Id = 3, Name = "High" },
            new() { Id = 4, Name = "Critical" }
        };

        _mockLookupRepository
            .Setup(r => r.GetPrioritiesAsync())
            .ReturnsAsync(expectedPriorities);

        // Act
        var result = await _sut.GetPrioritiesAsync();

        // Assert
        result.Should().HaveCount(4);
        result[0].Name.Should().Be("Low");
        result[3].Name.Should().Be("Critical");
    }

    [Fact]
    public async Task GetPrioritiesAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _mockLookupRepository
            .Setup(r => r.GetPrioritiesAsync())
            .ReturnsAsync(new List<LookupItemDto>());

        // Act
        var result = await _sut.GetPrioritiesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetStatusesAsync Tests

    [Fact]
    public async Task GetStatusesAsync_ReturnsStatusesList()
    {
        // Arrange
        var expectedStatuses = new List<LookupItemDto>
        {
            new() { Id = 1, Name = "Open" },
            new() { Id = 2, Name = "In Progress" },
            new() { Id = 3, Name = "Resolved" },
            new() { Id = 4, Name = "Closed" }
        };

        _mockLookupRepository
            .Setup(r => r.GetStatusesAsync())
            .ReturnsAsync(expectedStatuses);

        // Act
        var result = await _sut.GetStatusesAsync();

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(s => s.Name == "Open");
        result.Should().Contain(s => s.Name == "Closed");
    }

    #endregion

    #region GetCategoriesAsync Tests

    [Fact]
    public async Task GetCategoriesAsync_ReturnsCategoriesList()
    {
        // Arrange
        var expectedCategories = new List<CategoryLookupDto>
        {
            new() { Id = 1, Name = "Software", ParentId = null },
            new() { Id = 2, Name = "Hardware", ParentId = null },
            new() { Id = 3, Name = "Network", ParentId = null }
        };

        _mockLookupRepository
            .Setup(r => r.GetCategoriesAsync())
            .ReturnsAsync(expectedCategories);

        // Act
        var result = await _sut.GetCategoriesAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Software");
    }

    #endregion

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_ReturnsTeamsList()
    {
        // Arrange
        var expectedTeams = new List<LookupItemDto>
        {
            new() { Id = 1, Name = "IT Support" },
            new() { Id = 2, Name = "Development" }
        };

        _mockLookupRepository
            .Setup(r => r.GetTeamsAsync())
            .ReturnsAsync(expectedTeams);

        // Act
        var result = await _sut.GetTeamsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("IT Support");
    }

    #endregion

    #region GetAgentsAsync Tests

    [Fact]
    public async Task GetAgentsAsync_ReturnsAgentsList()
    {
        // Arrange
        var expectedAgents = new List<AgentLookupDto>
        {
            new() { Id = 1, Name = "John Doe", Email = "john@example.com" },
            new() { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
        };

        _mockLookupRepository
            .Setup(r => r.GetAgentsAsync())
            .ReturnsAsync(expectedAgents);

        // Act
        var result = await _sut.GetAgentsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Email.Should().Be("john@example.com");
    }

    #endregion

    #region GetRolesAsync Tests

    [Fact]
    public async Task GetRolesAsync_ReturnsRolesList()
    {
        // Arrange
        var expectedRoles = new List<LookupItemDto>
        {
            new() { Id = 1, Name = "Administrator" },
            new() { Id = 2, Name = "Manager" },
            new() { Id = 3, Name = "Agent" },
            new() { Id = 4, Name = "User" }
        };

        _mockLookupRepository
            .Setup(r => r.GetRolesAsync())
            .ReturnsAsync(expectedRoles);

        // Act
        var result = await _sut.GetRolesAsync();

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(r => r.Name == "Administrator");
    }

    #endregion

    #region GetAllLookupsAsync Tests

    [Fact]
    public async Task GetAllLookupsAsync_ReturnsAllLookupsInParallel()
    {
        // Arrange
        var priorities = new List<LookupItemDto> { new() { Id = 1, Name = "High" } };
        var statuses = new List<LookupItemDto> { new() { Id = 1, Name = "Open" } };
        var categories = new List<CategoryLookupDto> { new() { Id = 1, Name = "Software" } };
        var teams = new List<LookupItemDto> { new() { Id = 1, Name = "IT Support" } };
        var agents = new List<AgentLookupDto> { new() { Id = 1, Name = "John Doe" } };

        _mockLookupRepository.Setup(r => r.GetPrioritiesAsync()).ReturnsAsync(priorities);
        _mockLookupRepository.Setup(r => r.GetStatusesAsync()).ReturnsAsync(statuses);
        _mockLookupRepository.Setup(r => r.GetCategoriesAsync()).ReturnsAsync(categories);
        _mockLookupRepository.Setup(r => r.GetTeamsAsync()).ReturnsAsync(teams);
        _mockLookupRepository.Setup(r => r.GetAgentsAsync()).ReturnsAsync(agents);

        // Act
        var result = await _sut.GetAllLookupsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Priorities.Should().HaveCount(1);
        result.Statuses.Should().HaveCount(1);
        result.Categories.Should().HaveCount(1);
        result.Teams.Should().HaveCount(1);
        result.Agents.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllLookupsAsync_CallsAllRepositoryMethods()
    {
        // Arrange
        _mockLookupRepository.Setup(r => r.GetPrioritiesAsync()).ReturnsAsync(new List<LookupItemDto>());
        _mockLookupRepository.Setup(r => r.GetStatusesAsync()).ReturnsAsync(new List<LookupItemDto>());
        _mockLookupRepository.Setup(r => r.GetCategoriesAsync()).ReturnsAsync(new List<CategoryLookupDto>());
        _mockLookupRepository.Setup(r => r.GetTeamsAsync()).ReturnsAsync(new List<LookupItemDto>());
        _mockLookupRepository.Setup(r => r.GetAgentsAsync()).ReturnsAsync(new List<AgentLookupDto>());

        // Act
        await _sut.GetAllLookupsAsync();

        // Assert
        _mockLookupRepository.Verify(r => r.GetPrioritiesAsync(), Times.Once);
        _mockLookupRepository.Verify(r => r.GetStatusesAsync(), Times.Once);
        _mockLookupRepository.Verify(r => r.GetCategoriesAsync(), Times.Once);
        _mockLookupRepository.Verify(r => r.GetTeamsAsync(), Times.Once);
        _mockLookupRepository.Verify(r => r.GetAgentsAsync(), Times.Once);
    }

    #endregion
}
