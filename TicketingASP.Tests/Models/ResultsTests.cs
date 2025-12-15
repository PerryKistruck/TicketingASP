namespace TicketingASP.Tests.Models;

/// <summary>
/// Unit tests for OperationResult and PagedResult classes
/// </summary>
public class ResultsTests
{
    #region OperationResult Tests

    [Fact]
    public void OperationResult_SuccessResult_SetsCorrectProperties()
    {
        // Act
        var result = OperationResult.SuccessResult("Custom message");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Custom message");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void OperationResult_SuccessResult_WithDefaultMessage()
    {
        // Act
        var result = OperationResult.SuccessResult();

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Operation completed successfully");
    }

    [Fact]
    public void OperationResult_FailureResult_SetsCorrectProperties()
    {
        // Act
        var result = OperationResult.FailureResult("Error occurred", "Detail 1", "Detail 2");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Error occurred");
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Detail 1");
        result.Errors.Should().Contain("Detail 2");
    }

    [Fact]
    public void OperationResult_FailureResult_WithNoAdditionalErrors()
    {
        // Act
        var result = OperationResult.FailureResult("Error occurred");

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region OperationResult<T> Tests

    [Fact]
    public void OperationResultT_SuccessResult_SetsDataCorrectly()
    {
        // Arrange
        var data = new TicketDetailDto { Id = 1, Title = "Test" };

        // Act
        var result = OperationResult<TicketDetailDto>.SuccessResult(data, "Created");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(1);
        result.Message.Should().Be("Created");
    }

    [Fact]
    public void OperationResultT_FailureResult_HasNullData()
    {
        // Act
        var result = OperationResult<TicketDetailDto>.FailureResult("Not found");

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
    }

    [Fact]
    public void OperationResultT_SuccessResult_WithIntData()
    {
        // Act
        var result = OperationResult<int>.SuccessResult(42, "ID generated");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(42);
    }

    #endregion

    #region PagedResult Tests

    [Fact]
    public void PagedResult_CalculatesTotalPagesCorrectly()
    {
        // Arrange
        var result = new PagedResult<TicketListDto>
        {
            PageSize = 10,
            TotalCount = 25
        };

        // Assert
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void PagedResult_TotalPages_WithExactDivision()
    {
        // Arrange
        var result = new PagedResult<TicketListDto>
        {
            PageSize = 10,
            TotalCount = 20
        };

        // Assert
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public void PagedResult_TotalPages_WithZeroCount()
    {
        // Arrange
        var result = new PagedResult<TicketListDto>
        {
            PageSize = 10,
            TotalCount = 0
        };

        // Assert
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void PagedResult_HasPreviousPage_ReturnsFalseForFirstPage()
    {
        // Arrange
        var result = new PagedResult<TicketListDto>
        {
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 50
        };

        // Assert
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_HasPreviousPage_ReturnsTrueForSecondPage()
    {
        // Arrange
        var result = new PagedResult<TicketListDto>
        {
            PageNumber = 2,
            PageSize = 10,
            TotalCount = 50
        };

        // Assert
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_HasNextPage_ReturnsTrueWhenMorePagesExist()
    {
        // Arrange
        var result = new PagedResult<TicketListDto>
        {
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 50
        };

        // Assert
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_HasNextPage_ReturnsFalseOnLastPage()
    {
        // Arrange
        var result = new PagedResult<TicketListDto>
        {
            PageNumber = 5,
            PageSize = 10,
            TotalCount = 50
        };

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_PageAlias_MatchesPageNumber()
    {
        // Arrange
        var result = new PagedResult<TicketListDto>
        {
            PageNumber = 3
        };

        // Assert
        result.Page.Should().Be(result.PageNumber);
    }

    [Fact]
    public void PagedResult_Items_InitializesToEmptyList()
    {
        // Arrange
        var result = new PagedResult<TicketListDto>();

        // Assert
        result.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    #endregion
}
