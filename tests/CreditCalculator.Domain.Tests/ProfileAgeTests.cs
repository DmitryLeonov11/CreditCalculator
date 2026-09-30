using CreditCalculator.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CreditCalculator.Domain.Tests;

public class ProfileAgeTests
{
    [Fact]
    public void GetAgeOn_BeforeBirthdayInYear_ReturnsAgeMinusOne()
    {
        var profile = new Profile { BirthDate = new DateOnly(1990, 5, 15) };

        profile.GetAgeOn(new DateOnly(2026, 5, 14)).Should().Be(35);
        profile.GetAgeOn(new DateOnly(2026, 5, 15)).Should().Be(36);
    }

    [Fact]
    public void GetAgeOn_LeapYearBirthdayInNonLeapYear_UsesFeb28Rule()
    {
        var profile = new Profile { BirthDate = new DateOnly(2000, 2, 29) };

        profile.GetAgeOn(new DateOnly(2025, 2, 27)).Should().Be(24);
        profile.GetAgeOn(new DateOnly(2025, 2, 28)).Should().Be(25);
        profile.GetAgeOn(new DateOnly(2025, 3, 1)).Should().Be(25);
    }
}
