using System;
using SoliloClock.Models;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class ScheduleCalculatorTests
{
    private static readonly ScheduleCalculator UtcCalculator = new(TimeZoneInfo.Utc);

    [Fact]
    public void FifteenMinutes_LandsOnNextQuarterHour()
    {
        var after = new DateTimeOffset(2026, 11, 21, 10, 7, 0, TimeSpan.Zero);

        var next = UtcCalculator.NextAfter(after, AnnouncementInterval.Minutes15, null);

        Assert.Equal(new DateTimeOffset(2026, 11, 21, 10, 15, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void ThirtyMinutes_LandsOnHalfHour()
    {
        var after = new DateTimeOffset(2026, 11, 21, 10, 31, 0, TimeSpan.Zero);

        var next = UtcCalculator.NextAfter(after, AnnouncementInterval.Minutes30, null);

        Assert.Equal(new DateTimeOffset(2026, 11, 21, 11, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void SixtyMinutes_LandsOnTheHour()
    {
        var after = new DateTimeOffset(2026, 11, 21, 10, 7, 0, TimeSpan.Zero);

        var next = UtcCalculator.NextAfter(after, AnnouncementInterval.Minutes60, null);

        Assert.Equal(new DateTimeOffset(2026, 11, 21, 11, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void ExactlyOnBoundary_SchedulesTheFollowingBoundary()
    {
        var boundary = new DateTimeOffset(2026, 11, 21, 10, 15, 0, TimeSpan.Zero);

        var next = UtcCalculator.NextAfter(boundary, AnnouncementInterval.Minutes15, null);

        Assert.Equal(new DateTimeOffset(2026, 11, 21, 10, 30, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void MidnightRollover_MovesToNextDay()
    {
        var after = new DateTimeOffset(2026, 11, 21, 23, 50, 0, TimeSpan.Zero);

        var next = UtcCalculator.NextAfter(after, AnnouncementInterval.Minutes15, null);

        Assert.Equal(new DateTimeOffset(2026, 11, 22, 0, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void FortyFiveMinutes_IsAnchoredToSelectionMoment()
    {
        var anchor = new DateTimeOffset(2026, 11, 21, 10, 7, 0, TimeSpan.Zero);

        var first = UtcCalculator.NextAfter(anchor, AnnouncementInterval.Minutes45, anchor);
        var second = UtcCalculator.NextAfter(first, AnnouncementInterval.Minutes45, anchor);

        Assert.Equal(new DateTimeOffset(2026, 11, 21, 10, 52, 0, TimeSpan.Zero), first);
        Assert.Equal(new DateTimeOffset(2026, 11, 21, 11, 37, 0, TimeSpan.Zero), second);
    }

    [Fact]
    public void FortyFiveMinutes_WithoutAnchor_StartsFromNow()
    {
        var after = new DateTimeOffset(2026, 11, 21, 10, 7, 0, TimeSpan.Zero);

        var next = UtcCalculator.NextAfter(after, AnnouncementInterval.Minutes45, null);

        Assert.Equal(new DateTimeOffset(2026, 11, 21, 10, 52, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void SpringForward_SkipsTheNonexistentWallClockHour()
    {
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return;
        }

        var calculator = new ScheduleCalculator(zone);
        var after = new DateTimeOffset(2026, 3, 8, 1, 50, 0, TimeSpan.FromHours(-5));

        var next = calculator.NextAfter(after, AnnouncementInterval.Minutes15, null);

        Assert.Equal(new DateTime(2026, 3, 8, 7, 0, 0, DateTimeKind.Utc), next.UtcDateTime);
        Assert.Equal(TimeSpan.FromHours(-4), next.Offset);
    }

    [Fact]
    public void FallBack_DoesNotRepeatTheAmbiguousWallClockBoundaryTwice()
    {
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return;
        }

        var calculator = new ScheduleCalculator(zone);
        var firstPass = new DateTimeOffset(2026, 11, 1, 1, 5, 0, TimeSpan.FromHours(-4));

        var next = calculator.NextAfter(firstPass, AnnouncementInterval.Minutes15, null);

        // The earlier (EDT) occurrence of 01:15 is selected, not the repeated EST one.
        Assert.Equal(TimeSpan.FromHours(-4), next.Offset);
        Assert.Equal(1, next.Hour);
        Assert.Equal(15, next.Minute);
    }
}
