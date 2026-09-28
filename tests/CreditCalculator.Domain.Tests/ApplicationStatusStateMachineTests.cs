using CreditCalculator.Domain.Entities;
using CreditCalculator.Domain.Enums;
using CreditCalculator.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CreditCalculator.Domain.Tests;

public class ApplicationStatusStateMachineTests
{
    [Theory]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Scoring)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Scoring, ApplicationStatus.AutoApproved)]
    [InlineData(ApplicationStatus.Scoring, ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Scoring, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Scoring, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.AutoApproved, ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.AutoApproved, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Withdrawn)]
    public void ChangeStatus_ValidTransition_UpdatesStatus(ApplicationStatus initialStatus, ApplicationStatus targetStatus)
    {
        // Arrange
        var application = new Application { Status = initialStatus };

        // Act
        application.ChangeStatus(targetStatus);

        // Assert
        application.Status.Should().Be(targetStatus);
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.AutoApproved)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.AutoApproved, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.AutoApproved, ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Approved, ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Approved, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Withdrawn, ApplicationStatus.Draft)]
    public void ChangeStatus_InvalidTransition_ThrowsBusinessRuleExceptionWith409(ApplicationStatus initialStatus, ApplicationStatus targetStatus)
    {
        // Arrange
        var application = new Application { Status = initialStatus };

        // Act
        var act = () => application.ChangeStatus(targetStatus);

        // Assert
        var exception = act.Should().Throw<BusinessRuleException>().Which;
        exception.StatusCode.Should().Be(409);
        application.Status.Should().Be(initialStatus);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ChangeStatus_SameStatus_DoesNotThrow()
    {
        // Arrange
        var application = new Application { Status = ApplicationStatus.UnderReview };

        // Act
        var act = () => application.ChangeStatus(ApplicationStatus.UnderReview);

        // Assert
        act.Should().NotThrow();
        application.Status.Should().Be(ApplicationStatus.UnderReview);
    }
}
