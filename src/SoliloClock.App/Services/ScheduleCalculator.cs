using System;
using SoliloClock.Models;

namespace SoliloClock.Services;

/// <summary>
/// Pure schedule math. 15/30/60 align to local wall-clock boundaries;
/// 45 is anchored to the moment it was selected and counts elapsed minutes.
/// All methods return a moment strictly after the supplied instant.
/// </summary>
public sealed class ScheduleCalculator
{
    private readonly TimeZoneInfo _timeZone;

    public ScheduleCalculator(TimeZoneInfo? timeZone = null)
    {
        _timeZone = timeZone ?? TimeZoneInfo.Local;
    }

    public DateTimeOffset NextAfter(
        DateTimeOffset after,
        AnnouncementInterval interval,
        DateTimeOffset? anchor45)
    {
        if (interval == AnnouncementInterval.Minutes45)
        {
            var anchor = anchor45 ?? after;
            if (after < anchor)
            {
                return anchor.AddMinutes(45);
            }

            var elapsedMinutes = (after - anchor).TotalMinutes;
            var steps = (long)Math.Floor(elapsedMinutes / 45d) + 1;
            return anchor.AddMinutes(45 * steps);
        }

        var step = (int)interval;
        var localAfter = TimeZoneInfo.ConvertTime(after, _timeZone);
        var firstDay = localAfter.Date;

        // Two local days always contain at least one convertible boundary and
        // cover the spring-forward gap as well as the repeated autumn hour.
        for (var dayOffset = 0; dayOffset <= 2; dayOffset++)
        {
            var day = firstDay.AddDays(dayOffset);
            for (var minute = 0; minute < 24 * 60; minute += step)
            {
                var wallClock = day.AddMinutes(minute);
                if (TryConvert(wallClock, out var candidate) && candidate > after)
                {
                    return candidate;
                }
            }
        }

        // Defensive fallback; unreachable for a valid zone and interval.
        return after.AddMinutes(step);
    }

    private bool TryConvert(DateTime wallClock, out DateTimeOffset result)
    {
        result = default;

        // A boundary inside a spring-forward gap does not exist. Move to the
        // first valid wall time, which is the resumption of the next hour.
        if (_timeZone.IsInvalidTime(wallClock))
        {
            wallClock = wallClock.AddHours(1);
        }

        if (_timeZone.IsAmbiguousTime(wallClock))
        {
            // Choose the earliest occurrence of the repeated hour so a
            // duplicate wall-clock boundary is never announced twice.
            var offsets = _timeZone.GetAmbiguousTimeOffsets(wallClock);
            var earliest = TimeSpan.MinValue;
            foreach (var offset in offsets)
            {
                if (offset > earliest)
                {
                    earliest = offset;
                }
            }

            result = new DateTimeOffset(DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified), earliest);
            return true;
        }

        try
        {
            result = new DateTimeOffset(
                DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified),
                _timeZone.GetUtcOffset(wallClock));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
