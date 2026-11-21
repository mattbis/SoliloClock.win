using System;
using SoliloClock.Models;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class AnnouncementScheduleTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 21, 10, 7, 0, TimeSpan.Zero);

    private static (FakeClock Clock, AnnouncementSchedule Schedule) CreateAt(DateTimeOffset now)
    {
        var clock = new FakeClock(now);
        var schedule = new AnnouncementSchedule(clock, new ScheduleCalculator(TimeZoneInfo.Utc));
        schedule.Configure(AnnouncementInterval.Minutes15, null);
        return (clock, schedule);
    }

    [Fact]
    public void Configure_ComputesNextBoundary()
    {
        var (_, schedule) = CreateAt(Start);

        Assert.Equal(new DateTimeOffset(2026, 11, 21, 10, 15, 0, TimeSpan.Zero), schedule.Next);
    }

    [Fact]
    public void RepeatedTimerCallbacks_AnnounceEachBoundaryOnce()
    {
        var (clock, schedule) = CreateAt(Start);

        clock.Now = new DateTimeOffset(2026, 11, 21, 10, 15, 0, TimeSpan.Zero);
        Assert.True(schedule.TryConsumeDue(out var first));
        Assert.Equal(new DateTimeOffset(2026, 11, 21, 10, 15, 0, TimeSpan.Zero), first);

        Assert.False(schedule.TryConsumeDue(out _));
        Assert.Equal(new DateTimeOffset(2026, 11, 21, 10, 30, 0, TimeSpan.Zero), schedule.Next);

        clock.Now = new DateTimeOffset(2026, 11, 21, 10, 30, 2, TimeSpan.Zero);
        Assert.True(schedule.TryConsumeDue(out var second));
        Assert.Equal(new DateTimeOffset(2026, 11, 21, 10, 30, 0, TimeSpan.Zero), second);
    }

    [Fact]
    public void MissedBoundaries_AreSkippedAndNotReplayed()
    {
        var (clock, schedule) = CreateAt(Start);

        clock.Now = new DateTimeOffset(2026, 11, 21, 12, 17, 0, TimeSpan.Zero);

        Assert.False(schedule.TryConsumeDue(out _));
        Assert.Equal(new DateTimeOffset(2026, 11, 21, 12, 30, 0, TimeSpan.Zero), schedule.Next);

        // No backlog: the skipped 10:15-12:15 boundaries are gone for good.
        clock.Now = new DateTimeOffset(2026, 11, 21, 12, 30, 10, TimeSpan.Zero);
        Assert.True(schedule.TryConsumeDue(out var due));
        Assert.Equal(new DateTimeOffset(2026, 11, 21, 12, 30, 0, TimeSpan.Zero), due);
    }

    [Fact]
    public void SlightlyLateCallback_StillAnnounces()
    {
        var (clock, schedule) = CreateAt(Start);
        schedule.MissedTolerance = TimeSpan.FromSeconds(90);

        clock.Now = new DateTimeOffset(2026, 11, 21, 10, 16, 0, TimeSpan.Zero);

        Assert.True(schedule.TryConsumeDue(out var due));
        Assert.Equal(new DateTimeOffset(2026, 11, 21, 10, 15, 0, TimeSpan.Zero), due);
    }

    [Fact]
    public void Reschedule_AfterClockMovesBackwards_RecomputesFromNow()
    {
        var (clock, schedule) = CreateAt(Start);
        Assert.Equal(new DateTimeOffset(2026, 11, 21, 10, 15, 0, TimeSpan.Zero), schedule.Next);

        clock.Now = new DateTimeOffset(2026, 11, 21, 9, 40, 0, TimeSpan.Zero);
        schedule.Reschedule();

        Assert.Equal(new DateTimeOffset(2026, 11, 21, 9, 45, 0, TimeSpan.Zero), schedule.Next);
    }

    [Fact]
    public void Configure_FortyFive_UsesAnchor()
    {
        var clock = new FakeClock(Start);
        var schedule = new AnnouncementSchedule(clock, new ScheduleCalculator(TimeZoneInfo.Utc));
        var anchor = Start;

        schedule.Configure(AnnouncementInterval.Minutes45, anchor);

        Assert.Equal(new DateTimeOffset(2026, 11, 21, 10, 52, 0, TimeSpan.Zero), schedule.Next);
    }
}
