using System.ComponentModel.DataAnnotations;

namespace TicketingASP.Tests.Models;

/// <summary>
/// Unit tests for DTO validation attributes
/// </summary>
public class DtoValidationTests
{
    #region CreateTicketDto Validation Tests

    [Fact]
    public void CreateTicketDto_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new CreateTicketDto
        {
            Title = "Valid Ticket Title",
            Description = "Some description",
            PriorityId = 1
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void CreateTicketDto_WithEmptyTitle_FailsValidation()
    {
        // Arrange
        var dto = new CreateTicketDto
        {
            Title = "",
            PriorityId = 1
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void CreateTicketDto_WithTitleTooShort_FailsValidation()
    {
        // Arrange
        var dto = new CreateTicketDto
        {
            Title = "Hi", // Less than 5 characters
            PriorityId = 1
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void CreateTicketDto_WithTitleTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateTicketDto
        {
            Title = new string('x', 256), // More than 255 characters
            PriorityId = 1
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void CreateTicketDto_WithZeroPriorityId_PassesValidation()
    {
        // Arrange - Note: [Required] on int defaults to 0 which is valid for the attribute
        // The business logic should validate that PriorityId refers to an existing priority
        var dto = new CreateTicketDto
        {
            Title = "Valid Title",
            PriorityId = 0  // Defaults to 0, which passes [Required] validation for value types
        };

        // Act
        var results = ValidateModel(dto);

        // Assert - No validation errors since 0 is a valid int value
        // Business logic should handle checking if priority exists in database
        results.Should().BeEmpty();
    }

    #endregion

    #region RegisterUserDto Validation Tests

    [Fact]
    public void RegisterUserDto_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "test@example.com",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void RegisterUserDto_WithInvalidEmail_FailsValidation()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "invalid-email",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void RegisterUserDto_WithWeakPassword_FailsValidation()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "test@example.com",
            Password = "weak", // Too short, no number, no special char
            ConfirmPassword = "weak",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void RegisterUserDto_WithMismatchedPasswords_FailsValidation()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "test@example.com",
            Password = "Test123!@#",
            ConfirmPassword = "DifferentPassword123!@#",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("ConfirmPassword"));
    }

    [Fact]
    public void RegisterUserDto_WithMissingFirstName_FailsValidation()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "test@example.com",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#",
            FirstName = "",
            LastName = "Doe"
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("FirstName"));
    }

    #endregion

    #region LoginDto Validation Tests

    [Fact]
    public void LoginDto_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = "anypassword"
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void LoginDto_WithEmptyEmail_FailsValidation()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "",
            Password = "password"
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void LoginDto_WithEmptyPassword_FailsValidation()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = ""
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
    }

    #endregion

    #region ChangePasswordDto Validation Tests

    [Fact]
    public void ChangePasswordDto_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass123!@#",
            ConfirmNewPassword = "NewPass123!@#"
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void ChangePasswordDto_WithMismatchedNewPasswords_FailsValidation()
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass123!@#",
            ConfirmNewPassword = "DifferentPass123!@#"
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("ConfirmNewPassword"));
    }

    #endregion

    #region UpdateUserDto Validation Tests

    [Fact]
    public void UpdateUserDto_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new UpdateUserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Phone = "+1234567890"
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void UpdateUserDto_WithInvalidAvatarUrl_FailsValidation()
    {
        // Arrange
        var dto = new UpdateUserDto
        {
            FirstName = "John",
            LastName = "Doe",
            AvatarUrl = "not-a-valid-url"
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("AvatarUrl"));
    }

    #endregion

    #region TicketFilterDto Tests

    [Fact]
    public void TicketFilterDto_DefaultValues_AreCorrect()
    {
        // Arrange
        var dto = new TicketFilterDto();

        // Assert
        dto.PageNumber.Should().Be(1);
        dto.PageSize.Should().Be(20);
        dto.IncludeClosed.Should().BeFalse();
        dto.StatusId.Should().BeNull();
        dto.PriorityId.Should().BeNull();
        dto.CategoryId.Should().BeNull();
        dto.AssignedToId.Should().BeNull();
        dto.AssignedTeamId.Should().BeNull();
        dto.RequesterId.Should().BeNull();
        dto.Search.Should().BeNull();
    }

    [Fact]
    public void TicketFilterDto_IncludeClosed_CanBeSetToTrue()
    {
        // Arrange
        var dto = new TicketFilterDto
        {
            IncludeClosed = true
        };

        // Assert
        dto.IncludeClosed.Should().BeTrue();
    }

    [Fact]
    public void TicketFilterDto_WithAllFilters_PassesValidation()
    {
        // Arrange
        var dto = new TicketFilterDto
        {
            PageNumber = 1,
            PageSize = 50,
            Search = "test search",
            StatusId = 1,
            PriorityId = 2,
            CategoryId = 3,
            AssignedToId = 4,
            AssignedTeamId = 5,
            RequesterId = 6,
            DateFrom = DateTime.UtcNow.AddDays(-30),
            DateTo = DateTime.UtcNow,
            IncludeClosed = true
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true);
        return validationResults;
    }

    #endregion
}
