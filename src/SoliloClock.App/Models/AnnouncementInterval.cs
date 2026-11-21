using System;

namespace SoliloClock.Models;

/// <summary>
/// The four supported announcement cadences, in minutes.
/// </summary>
public enum AnnouncementInterval
{
    Minutes15 = 15,
    Minutes30 = 30,
    Minutes45 = 45,
    Minutes60 = 60,
}

public static class AnnouncementIntervals
{
    public static readonly AnnouncementInterval[] All =
    {
        AnnouncementInterval.Minutes15,
        AnnouncementInterval.Minutes30,
        AnnouncementInterval.Minutes45,
        AnnouncementInterval.Minutes60,
    };

    public static bool IsSupported(int minutes) => minutes is 15 or 30 or 45 or 60;

    public static AnnouncementInterval FromMinutes(int minutes) =>
        IsSupported(minutes)
            ? (AnnouncementInterval)minutes
            : throw new ArgumentOutOfRangeException(nameof(minutes), minutes, "Only 15, 30, 45 and 60 are supported.");
}
