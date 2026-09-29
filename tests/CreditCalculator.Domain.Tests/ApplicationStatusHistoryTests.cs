using CreditCalculator.Domain.Entities;
using CreditCalculator.Domain.Enums;
using CreditCalculator.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CreditCalculator.Domain.Tests;

public class ApplicationStatusHistoryTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void ChangeStatus_ValidTransition_AppendsHistoryRecord()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var application = new Application
        {
            Id = appId,
            Status = ApplicationStatus.Draft
        };

        // Act
        var beforeTime = DateTimeOffset.UtcNow;
        application.ChangeStatus(ApplicationStatus.Submitted, changedByUserId: userId, comment: "Подача клиентом");
        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        application.Status.Should().Be(ApplicationStatus.Submitted);
        application.StatusHistory.Should().HaveCount(1);

        var history = application.StatusHistory[0];
        history.Id.Should().NotBeEmpty();
        history.ApplicationId.Should().Be(appId);
        history.FromStatus.Should().Be(ApplicationStatus.Draft);
        history.ToStatus.Should().Be(ApplicationStatus.Submitted);
        history.ChangedByUserId.Should().Be(userId);
        history.Comment.Should().Be("Подача клиентом");
        history.ChangedAt.Should().BeOnOrAfter(beforeTime).And.BeOnOrBefore(afterTime);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ChangeStatus_MultipleTransitions_AppendsHistorySequentially()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var application = new Application
        {
            Id = appId,
            Status = ApplicationStatus.Draft
        };

        // Act
        application.ChangeStatus(ApplicationStatus.Submitted);
        application.ChangeStatus(ApplicationStatus.Scoring);
        application.ChangeStatus(ApplicationStatus.UnderReview);
        application.ChangeStatus(ApplicationStatus.Approved, changedByUserId: employeeId, comment: "Одобрено кредитным комитетом");

        // Assert
        application.Status.Should().Be(ApplicationStatus.Approved);
        application.StatusHistory.Should().HaveCount(4);

        application.StatusHistory[0].FromStatus.Should().Be(ApplicationStatus.Draft);
        application.StatusHistory[0].ToStatus.Should().Be(ApplicationStatus.Submitted);

        application.StatusHistory[1].FromStatus.Should().Be(ApplicationStatus.Submitted);
        application.StatusHistory[1].ToStatus.Should().Be(ApplicationStatus.Scoring);

        application.StatusHistory[2].FromStatus.Should().Be(ApplicationStatus.Scoring);
        application.StatusHistory[2].ToStatus.Should().Be(ApplicationStatus.UnderReview);

        application.StatusHistory[3].FromStatus.Should().Be(ApplicationStatus.UnderReview);
        application.StatusHistory[3].ToStatus.Should().Be(ApplicationStatus.Approved);
        application.StatusHistory[3].ChangedByUserId.Should().Be(employeeId);
        application.StatusHistory[3].Comment.Should().Be("Одобрено кредитным комитетом");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ChangeStatus_InvalidTransition_DoesNotAppendHistory()
    {
        // Arrange
        var application = new Application
        {
            Status = ApplicationStatus.Draft
        };

        // Act
        var act = () => application.ChangeStatus(ApplicationStatus.Approved);

        // Assert
        act.Should().Throw<BusinessRuleException>();
        application.StatusHistory.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ChangeStatus_SameStatus_DoesNotAppendHistory()
    {
        // Arrange
        var application = new Application
        {
            Status = ApplicationStatus.Submitted
        };

        // Act
        application.ChangeStatus(ApplicationStatus.Submitted);

        // Assert
        application.StatusHistory.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AddStatusHistory_AppendsEntryDirectly()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var application = new Application
        {
            Id = appId,
            Status = ApplicationStatus.Draft
        };

        // Act
        var history = application.AddStatusHistory(null, ApplicationStatus.Draft, comment: "Черновик создан");

        // Assert
        application.StatusHistory.Should().ContainSingle();
        history.ApplicationId.Should().Be(appId);
        history.FromStatus.Should().BeNull();
        history.ToStatus.Should().Be(ApplicationStatus.Draft);
        history.Comment.Should().Be("Черновик создан");
    }
}
