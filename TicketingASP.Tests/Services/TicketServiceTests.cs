namespace TicketingASP.Tests.Services;

/// <summary>
/// Unit tests for TicketService
/// </summary>
public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _mockTicketRepository;
    private readonly Mock<ILogger<TicketService>> _mockLogger;
    private readonly TicketService _sut;

    public TicketServiceTests()
    {
        _mockTicketRepository = new Mock<ITicketRepository>();
        _mockLogger = new Mock<ILogger<TicketService>>();
        _sut = new TicketService(_mockTicketRepository.Object, _mockLogger.Object);
    }

    #region CreateTicketAsync Tests

    [Fact]
    public async Task CreateTicketAsync_WithValidDto_ReturnsSuccess()
    {
        // Arrange
        var createDto = new CreateTicketDto
        {
            Title = "Test Ticket",
            Description = "Test Description",
            PriorityId = 1,
            CategoryId = 1
        };
        var requesterId = 1;
        var expectedTicket = new TicketDetailDto
        {
            Id = 1,
            TicketNumber = "TKT-001",
            Title = "Test Ticket",
            Description = "Test Description"
        };

        _mockTicketRepository
            .Setup(r => r.CreateAsync(createDto, requesterId, null, null))
            .ReturnsAsync(OperationResult<TicketDetailDto>.SuccessResult(expectedTicket));

        // Act
        var result = await _sut.CreateTicketAsync(createDto, requesterId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Test Ticket");
        _mockTicketRepository.Verify(r => r.CreateAsync(createDto, requesterId, null, null), Times.Once);
    }

    [Fact]
    public async Task CreateTicketAsync_WithCreatedByUser_PassesCreatedByToRepository()
    {
        // Arrange
        var createDto = new CreateTicketDto { Title = "Test", PriorityId = 1 };
        var requesterId = 1;
        var createdBy = 5;
        var ipAddress = "192.168.1.1";

        _mockTicketRepository
            .Setup(r => r.CreateAsync(createDto, requesterId, createdBy, ipAddress))
            .ReturnsAsync(OperationResult<TicketDetailDto>.SuccessResult(new TicketDetailDto { Id = 1 }));

        // Act
        await _sut.CreateTicketAsync(createDto, requesterId, createdBy, ipAddress);

        // Assert
        _mockTicketRepository.Verify(r => r.CreateAsync(createDto, requesterId, createdBy, ipAddress), Times.Once);
    }

    [Fact]
    public async Task CreateTicketAsync_WhenRepositoryFails_ReturnsFailure()
    {
        // Arrange
        var createDto = new CreateTicketDto { Title = "Test", PriorityId = 1 };
        var errorMessage = "Database error";

        _mockTicketRepository
            .Setup(r => r.CreateAsync(It.IsAny<CreateTicketDto>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>()))
            .ReturnsAsync(OperationResult<TicketDetailDto>.FailureResult(errorMessage));

        // Act
        var result = await _sut.CreateTicketAsync(createDto, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(errorMessage);
    }

    #endregion

    #region GetTicketByIdAsync Tests

    [Fact]
    public async Task GetTicketByIdAsync_WithValidId_ReturnsTicket()
    {
        // Arrange
        var ticketId = 1;
        var expectedTicket = new TicketDetailDto
        {
            Id = ticketId,
            TicketNumber = "TKT-001",
            Title = "Test Ticket"
        };

        _mockTicketRepository
            .Setup(r => r.GetByIdAsync(ticketId))
            .ReturnsAsync(expectedTicket);

        // Act
        var result = await _sut.GetTicketByIdAsync(ticketId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(ticketId);
        result.Title.Should().Be("Test Ticket");
    }

    [Fact]
    public async Task GetTicketByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var ticketId = 999;

        _mockTicketRepository
            .Setup(r => r.GetByIdAsync(ticketId))
            .ReturnsAsync((TicketDetailDto?)null);

        // Act
        var result = await _sut.GetTicketByIdAsync(ticketId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateTicketAsync Tests

    [Fact]
    public async Task UpdateTicketAsync_WithValidDto_ReturnsSuccess()
    {
        // Arrange
        var ticketId = 1;
        var updateDto = new UpdateTicketDto { Title = "Updated Title" };
        var updatedBy = 1;

        _mockTicketRepository
            .Setup(r => r.UpdateAsync(ticketId, updateDto, updatedBy, null))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.UpdateTicketAsync(ticketId, updateDto, updatedBy);

        // Assert
        result.Success.Should().BeTrue();
        _mockTicketRepository.Verify(r => r.UpdateAsync(ticketId, updateDto, updatedBy, null), Times.Once);
    }

    [Fact]
    public async Task UpdateTicketAsync_WhenRepositoryFails_ReturnsFailure()
    {
        // Arrange
        var ticketId = 1;
        var updateDto = new UpdateTicketDto { Title = "Updated" };
        var errorMessage = "Ticket not found";

        _mockTicketRepository
            .Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateTicketDto>(), It.IsAny<int>(), It.IsAny<string?>()))
            .ReturnsAsync(OperationResult.FailureResult(errorMessage));

        // Act
        var result = await _sut.UpdateTicketAsync(ticketId, updateDto, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(errorMessage);
    }

    #endregion

    #region DeleteTicketAsync Tests

    [Fact]
    public async Task DeleteTicketAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var ticketId = 1;
        var deletedBy = 1;

        _mockTicketRepository
            .Setup(r => r.DeleteAsync(ticketId, deletedBy, null))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.DeleteTicketAsync(ticketId, deletedBy);

        // Assert
        result.Success.Should().BeTrue();
        _mockTicketRepository.Verify(r => r.DeleteAsync(ticketId, deletedBy, null), Times.Once);
    }

    #endregion

    #region GetTicketsAsync Tests

    [Fact]
    public async Task GetTicketsAsync_ReturnsPagedResult()
    {
        // Arrange
        var filter = new TicketFilterDto { PageNumber = 1, PageSize = 10 };
        var userId = 1;
        var userRole = "Admin";
        var expectedResult = new PagedResult<TicketListDto>
        {
            Items = new List<TicketListDto>
            {
                new() { Id = 1, Title = "Ticket 1" },
                new() { Id = 2, Title = "Ticket 2" }
            },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 2
        };

        _mockTicketRepository
            .Setup(r => r.GetListAsync(filter, userId, userRole))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.GetTicketsAsync(filter, userId, userRole);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    #endregion

    #region AddCommentAsync Tests

    [Fact]
    public async Task AddCommentAsync_WithValidData_ReturnsCommentId()
    {
        // Arrange
        var ticketId = 1;
        var userId = 1;
        var commentDto = new AddCommentDto { Content = "Test comment" };
        var expectedCommentId = 10;

        _mockTicketRepository
            .Setup(r => r.AddCommentAsync(ticketId, userId, commentDto, null))
            .ReturnsAsync(OperationResult<int>.SuccessResult(expectedCommentId));

        // Act
        var result = await _sut.AddCommentAsync(ticketId, userId, commentDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(expectedCommentId);
    }

    #endregion

    #region GetCommentsAsync Tests

    [Fact]
    public async Task GetCommentsAsync_ReturnsCommentsList()
    {
        // Arrange
        var ticketId = 1;
        var expectedComments = new List<TicketCommentDto>
        {
            new() { Id = 1, Content = "Comment 1" },
            new() { Id = 2, Content = "Comment 2" }
        };

        _mockTicketRepository
            .Setup(r => r.GetCommentsAsync(ticketId, false))
            .ReturnsAsync(expectedComments);

        // Act
        var result = await _sut.GetCommentsAsync(ticketId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCommentsAsync_WithIncludeInternal_ReturnsAllComments()
    {
        // Arrange
        var ticketId = 1;
        var expectedComments = new List<TicketCommentDto>
        {
            new() { Id = 1, Content = "Public Comment", IsInternal = false },
            new() { Id = 2, Content = "Internal Comment", IsInternal = true }
        };

        _mockTicketRepository
            .Setup(r => r.GetCommentsAsync(ticketId, true))
            .ReturnsAsync(expectedComments);

        // Act
        var result = await _sut.GetCommentsAsync(ticketId, includeInternal: true);

        // Assert
        result.Should().HaveCount(2);
        _mockTicketRepository.Verify(r => r.GetCommentsAsync(ticketId, true), Times.Once);
    }

    #endregion

    #region GetHistoryAsync Tests

    [Fact]
    public async Task GetHistoryAsync_ReturnsHistoryList()
    {
        // Arrange
        var ticketId = 1;
        var expectedHistory = new List<TicketHistoryDto>
        {
            new() { Id = 1, FieldName = "Status", OldValue = "Open", NewValue = "In Progress" }
        };

        _mockTicketRepository
            .Setup(r => r.GetHistoryAsync(ticketId))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _sut.GetHistoryAsync(ticketId);

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("Status");
    }

    #endregion

    #region AssignTicketAsync Tests

    [Fact]
    public async Task AssignTicketAsync_WithUserAndTeam_UpdatesTicket()
    {
        // Arrange
        var ticketId = 1;
        var assignedToId = 5;
        var assignedTeamId = 2;
        var updatedBy = 1;

        _mockTicketRepository
            .Setup(r => r.UpdateAsync(ticketId, It.Is<UpdateTicketDto>(d => 
                d.AssignedToId == assignedToId && d.AssignedTeamId == assignedTeamId), 
                updatedBy, null))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.AssignTicketAsync(ticketId, assignedToId, assignedTeamId, updatedBy);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AssignTicketAsync_WithNullValues_UpdatesTicketWithNulls()
    {
        // Arrange
        var ticketId = 1;
        var updatedBy = 1;

        _mockTicketRepository
            .Setup(r => r.UpdateAsync(ticketId, It.Is<UpdateTicketDto>(d => 
                d.AssignedToId == null && d.AssignedTeamId == null), 
                updatedBy, null))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.AssignTicketAsync(ticketId, null, null, updatedBy);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region ChangeStatusAsync Tests

    [Fact]
    public async Task ChangeStatusAsync_WithValidStatus_UpdatesTicket()
    {
        // Arrange
        var ticketId = 1;
        var statusId = 3;
        var updatedBy = 1;

        _mockTicketRepository
            .Setup(r => r.UpdateAsync(ticketId, It.Is<UpdateTicketDto>(d => d.StatusId == statusId), updatedBy, null))
            .ReturnsAsync(OperationResult.SuccessResult());

        // Act
        var result = await _sut.ChangeStatusAsync(ticketId, statusId, updatedBy);

        // Assert
        result.Success.Should().BeTrue();
        _mockTicketRepository.Verify(r => r.UpdateAsync(ticketId, 
            It.Is<UpdateTicketDto>(d => d.StatusId == statusId), updatedBy, null), Times.Once);
    }

    #endregion
}
