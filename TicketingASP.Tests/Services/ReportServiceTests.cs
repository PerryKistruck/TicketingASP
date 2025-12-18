namespace TicketingASP.Tests.Services;

/// <summary>
/// Unit tests for ReportService
/// </summary>
public class ReportServiceTests
{
    private readonly Mock<IReportRepository> _mockReportRepository;
    private readonly Mock<ILogger<ReportService>> _mockLogger;
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _mockReportRepository = new Mock<IReportRepository>();
        _mockLogger = new Mock<ILogger<ReportService>>();
        _sut = new ReportService(_mockReportRepository.Object, _mockLogger.Object);
    }

    #region GetDashboardSummaryAsync Tests

    [Fact]
    public async Task GetDashboardSummaryAsync_WithValidParams_ReturnsSummary()
    {
        // Arrange
        var userId = 1;
        var userRole = "Admin";
        var expectedSummary = new DashboardSummaryDto
        {
            TotalTickets = 100,
            OpenTickets = 30,
            PendingTickets = 25,
            ResolvedToday = 10
        };

        _mockReportRepository
            .Setup(r => r.GetDashboardSummaryAsync(userId, userRole, null))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _sut.GetDashboardSummaryAsync(userId, userRole);

        // Assert
        result.Should().NotBeNull();
        result!.TotalTickets.Should().Be(100);
        result.OpenTickets.Should().Be(30);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_WithTeamId_PassesTeamIdToRepository()
    {
        // Arrange
        var userId = 1;
        var userRole = "Manager";
        var teamId = 5;

        _mockReportRepository
            .Setup(r => r.GetDashboardSummaryAsync(userId, userRole, teamId))
            .ReturnsAsync(new DashboardSummaryDto());

        // Act
        await _sut.GetDashboardSummaryAsync(userId, userRole, teamId);

        // Assert
        _mockReportRepository.Verify(r => r.GetDashboardSummaryAsync(userId, userRole, teamId), Times.Once);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_WhenNoData_ReturnsNull()
    {
        // Arrange
        _mockReportRepository
            .Setup(r => r.GetDashboardSummaryAsync(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync((DashboardSummaryDto?)null);

        // Act
        var result = await _sut.GetDashboardSummaryAsync(1, "User");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetTicketsByStatusAsync Tests

    [Fact]
    public async Task GetTicketsByStatusAsync_ReturnsStatusBreakdown()
    {
        // Arrange
        var expectedData = new List<TicketsByStatusDto>
        {
            new() { StatusName = "Open", TicketCount = 30, Percentage = 30 },
            new() { StatusName = "In Progress", TicketCount = 25, Percentage = 25 },
            new() { StatusName = "Resolved", TicketCount = 45, Percentage = 45 }
        };

        _mockReportRepository
            .Setup(r => r.GetTicketsByStatusAsync(null, null))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _sut.GetTicketsByStatusAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].StatusName.Should().Be("Open");
    }

    [Fact]
    public async Task GetTicketsByStatusAsync_WithFilter_PassesDatesToRepository()
    {
        // Arrange
        var filter = new ReportFilterDto
        {
            DateFrom = DateTime.UtcNow.AddDays(-30),
            DateTo = DateTime.UtcNow
        };

        _mockReportRepository
            .Setup(r => r.GetTicketsByStatusAsync(filter.DateFrom, filter.DateTo))
            .ReturnsAsync(new List<TicketsByStatusDto>());

        // Act
        await _sut.GetTicketsByStatusAsync(filter);

        // Assert
        _mockReportRepository.Verify(r => r.GetTicketsByStatusAsync(filter.DateFrom, filter.DateTo), Times.Once);
    }

    #endregion

    #region GetTicketsByPriorityAsync Tests

    [Fact]
    public async Task GetTicketsByPriorityAsync_ReturnsPriorityBreakdown()
    {
        // Arrange
        var expectedData = new List<TicketsByPriorityDto>
        {
            new() { PriorityName = "Critical", TicketCount = 10 },
            new() { PriorityName = "High", TicketCount = 25 },
            new() { PriorityName = "Medium", TicketCount = 40 },
            new() { PriorityName = "Low", TicketCount = 25 }
        };

        _mockReportRepository
            .Setup(r => r.GetTicketsByPriorityAsync(null, null))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _sut.GetTicketsByPriorityAsync();

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(p => p.PriorityName == "Critical");
    }

    [Fact]
    public async Task GetTicketsByPriorityAsync_WithFilter_PassesDatesToRepository()
    {
        // Arrange
        var filter = new ReportFilterDto
        {
            DateFrom = DateTime.UtcNow.AddMonths(-1),
            DateTo = DateTime.UtcNow
        };

        _mockReportRepository
            .Setup(r => r.GetTicketsByPriorityAsync(filter.DateFrom, filter.DateTo))
            .ReturnsAsync(new List<TicketsByPriorityDto>());

        // Act
        await _sut.GetTicketsByPriorityAsync(filter);

        // Assert
        _mockReportRepository.Verify(r => r.GetTicketsByPriorityAsync(filter.DateFrom, filter.DateTo), Times.Once);
    }

    #endregion

    #region GetTicketsByCategoryAsync Tests

    [Fact]
    public async Task GetTicketsByCategoryAsync_ReturnsCategoryBreakdown()
    {
        // Arrange
        var expectedData = new List<TicketsByCategoryDto>
        {
            new() { CategoryName = "Software", TicketCount = 50, OpenCount = 20 },
            new() { CategoryName = "Hardware", TicketCount = 30, OpenCount = 10 },
            new() { CategoryName = "Network", TicketCount = 20, OpenCount = 5 }
        };

        _mockReportRepository
            .Setup(r => r.GetTicketsByCategoryAsync(null, null))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _sut.GetTicketsByCategoryAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].CategoryName.Should().Be("Software");
    }

    #endregion

    #region GetTeamPerformanceAsync Tests

    [Fact]
    public async Task GetTeamPerformanceAsync_ReturnsTeamPerformance()
    {
        // Arrange
        var expectedData = new List<TeamPerformanceDto>
        {
            new() { TeamName = "IT Support", AssignedTickets = 50, ResolvedTickets = 45, AvgResolutionHours = 24.5m },
            new() { TeamName = "Development", AssignedTickets = 30, ResolvedTickets = 28, AvgResolutionHours = 48.0m }
        };

        _mockReportRepository
            .Setup(r => r.GetTeamPerformanceAsync(null, null))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _sut.GetTeamPerformanceAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].TeamName.Should().Be("IT Support");
        result[0].AvgResolutionHours.Should().Be(24.5m);
    }

    #endregion

    #region GetAgentPerformanceAsync Tests

    [Fact]
    public async Task GetAgentPerformanceAsync_ReturnsAgentPerformance()
    {
        // Arrange
        var expectedData = new List<AgentPerformanceDto>
        {
            new() { UserName = "John Doe", TotalAssigned = 20, ResolvedCount = 18, AvgResolutionHours = 12.0m },
            new() { UserName = "Jane Smith", TotalAssigned = 15, ResolvedCount = 14, AvgResolutionHours = 8.5m }
        };

        _mockReportRepository
            .Setup(r => r.GetAgentPerformanceAsync(null, null, null))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _sut.GetAgentPerformanceAsync();

        // Assert
        result.Should().HaveCount(2);
        result[1].UserName.Should().Be("Jane Smith");
    }

    [Fact]
    public async Task GetAgentPerformanceAsync_WithTeamFilter_PassesTeamIdToRepository()
    {
        // Arrange
        var filter = new ReportFilterDto { TeamId = 5 };

        _mockReportRepository
            .Setup(r => r.GetAgentPerformanceAsync(5, null, null))
            .ReturnsAsync(new List<AgentPerformanceDto>());

        // Act
        await _sut.GetAgentPerformanceAsync(filter);

        // Assert
        _mockReportRepository.Verify(r => r.GetAgentPerformanceAsync(5, null, null), Times.Once);
    }

    #endregion

    #region GetTicketTrendAsync Tests

    [Fact]
    public async Task GetTicketTrendAsync_ReturnsTicketTrend()
    {
        // Arrange
        var expectedData = new List<TicketTrendDto>
        {
            new() { PeriodDate = DateTime.UtcNow.AddDays(-2), CreatedCount = 10, ResolvedCount = 8 },
            new() { PeriodDate = DateTime.UtcNow.AddDays(-1), CreatedCount = 12, ResolvedCount = 10 },
            new() { PeriodDate = DateTime.UtcNow, CreatedCount = 8, ResolvedCount = 9 }
        };

        _mockReportRepository
            .Setup(r => r.GetTicketTrendAsync("daily", null, null))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _sut.GetTicketTrendAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTicketTrendAsync_WithPeriodFilter_PassesPeriodToRepository()
    {
        // Arrange
        var filter = new ReportFilterDto { Period = "weekly" };

        _mockReportRepository
            .Setup(r => r.GetTicketTrendAsync("weekly", null, null))
            .ReturnsAsync(new List<TicketTrendDto>());

        // Act
        await _sut.GetTicketTrendAsync(filter);

        // Assert
        _mockReportRepository.Verify(r => r.GetTicketTrendAsync("weekly", null, null), Times.Once);
    }

    [Fact]
    public async Task GetTicketTrendAsync_WithNullFilter_UsesDailyPeriod()
    {
        // Arrange
        _mockReportRepository
            .Setup(r => r.GetTicketTrendAsync("daily", null, null))
            .ReturnsAsync(new List<TicketTrendDto>());

        // Act
        await _sut.GetTicketTrendAsync();

        // Assert
        _mockReportRepository.Verify(r => r.GetTicketTrendAsync("daily", null, null), Times.Once);
    }

    #endregion

    #region GetSlaComplianceAsync Tests

    [Fact]
    public async Task GetSlaComplianceAsync_ReturnsSlaCompliance()
    {
        // Arrange
        var expectedData = new List<SlaComplianceDto>
        {
            new() { PriorityName = "Critical", TotalTickets = 20, WithinSlaResponse = 18, ResponseCompliancePercent = 90 },
            new() { PriorityName = "High", TotalTickets = 50, WithinSlaResponse = 45, ResponseCompliancePercent = 90 }
        };

        _mockReportRepository
            .Setup(r => r.GetSlaComplianceAsync(null, null))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _sut.GetSlaComplianceAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].ResponseCompliancePercent.Should().Be(90);
    }

    #endregion

    #region GetFullDashboardAsync Tests

    [Fact]
    public async Task GetFullDashboardAsync_ReturnsAllDashboardData()
    {
        // Arrange
        var userId = 1;
        var userRole = "Admin";

        _mockReportRepository
            .Setup(r => r.GetDashboardSummaryAsync(userId, userRole, null))
            .ReturnsAsync(new DashboardSummaryDto { TotalTickets = 100 });
        _mockReportRepository
            .Setup(r => r.GetTicketsByStatusAsync(null, null))
            .ReturnsAsync(new List<TicketsByStatusDto> { new() { StatusName = "Open" } });
        _mockReportRepository
            .Setup(r => r.GetTicketsByPriorityAsync(null, null))
            .ReturnsAsync(new List<TicketsByPriorityDto> { new() { PriorityName = "High" } });
        _mockReportRepository
            .Setup(r => r.GetTicketTrendAsync("daily", null, null))
            .ReturnsAsync(new List<TicketTrendDto> { new() { CreatedCount = 10 } });
        _mockReportRepository
            .Setup(r => r.GetTeamPerformanceAsync(null, null))
            .ReturnsAsync(new List<TeamPerformanceDto> { new() { TeamName = "IT Support" } });

        // Act
        var result = await _sut.GetFullDashboardAsync(userId, userRole);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().NotBeNull();
        result.Summary!.TotalTickets.Should().Be(100);
        result.TicketsByStatus.Should().HaveCount(1);
        result.TicketsByPriority.Should().HaveCount(1);
        result.TicketTrend.Should().HaveCount(1);
        result.TeamPerformance.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFullDashboardAsync_WithTeamId_PassesTeamIdToSummary()
    {
        // Arrange
        var userId = 1;
        var userRole = "Manager";
        var teamId = 5;

        _mockReportRepository
            .Setup(r => r.GetDashboardSummaryAsync(userId, userRole, teamId))
            .ReturnsAsync(new DashboardSummaryDto());
        _mockReportRepository
            .Setup(r => r.GetTicketsByStatusAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TicketsByStatusDto>());
        _mockReportRepository
            .Setup(r => r.GetTicketsByPriorityAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TicketsByPriorityDto>());
        _mockReportRepository
            .Setup(r => r.GetTicketTrendAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TicketTrendDto>());
        _mockReportRepository
            .Setup(r => r.GetTeamPerformanceAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TeamPerformanceDto>());

        // Act
        await _sut.GetFullDashboardAsync(userId, userRole, teamId);

        // Assert
        _mockReportRepository.Verify(r => r.GetDashboardSummaryAsync(userId, userRole, teamId), Times.Once);
    }

    [Fact]
    public async Task GetFullDashboardAsync_CallsAllRepositoryMethods()
    {
        // Arrange
        _mockReportRepository
            .Setup(r => r.GetDashboardSummaryAsync(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(new DashboardSummaryDto());
        _mockReportRepository
            .Setup(r => r.GetTicketsByStatusAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TicketsByStatusDto>());
        _mockReportRepository
            .Setup(r => r.GetTicketsByPriorityAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TicketsByPriorityDto>());
        _mockReportRepository
            .Setup(r => r.GetTicketTrendAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TicketTrendDto>());
        _mockReportRepository
            .Setup(r => r.GetTeamPerformanceAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TeamPerformanceDto>());

        // Act
        await _sut.GetFullDashboardAsync(1, "Admin");

        // Assert
        _mockReportRepository.Verify(r => r.GetDashboardSummaryAsync(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Once);
        _mockReportRepository.Verify(r => r.GetTicketsByStatusAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Once);
        _mockReportRepository.Verify(r => r.GetTicketsByPriorityAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Once);
        _mockReportRepository.Verify(r => r.GetTicketTrendAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Once);
        _mockReportRepository.Verify(r => r.GetTeamPerformanceAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Once);
    }

    #endregion
}
