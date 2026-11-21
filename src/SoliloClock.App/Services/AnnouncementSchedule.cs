using System;
using SoliloClock.Models;

namespace SoliloClock.Services;

/// <summary>
/// Tracks the next due boundary. Each boundary can be consumed once, and
/// boundaries older than <see cref="MissedTolerance"/> are skipped rather than
/// replayed after sleep, resume or a clock change.
/// </summary>
public sealed class AnnouncementSchedule
{
    private const int MaxSkipIterations = 10_000;

    private readonly object _gate = new();
    private readonly IClock _clock;
    private readonly ScheduleCalculator _calculator;

    private AnnouncementInterval _interval = AnnouncementInterval.Minutes15;
    private DateTimeOffset? _anchor45;
    private DateTimeOffset? _next;

    public AnnouncementSchedule(IClock clock, ScheduleCalculator? calculator = null)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _calculator = calculator ?? new ScheduleCalculator();
    }

    /// <summary>How late a boundary may fire and still be announced.</summary>
    public TimeSpan MissedTolerance { get; set; } = TimeSpan.FromSeconds(90);

    public AnnouncementInterval Interval
    {
        get
        {
            lock (_gate)
            {
                return _interval;
            }
        }
    }

    public DateTimeOffset? Next
    {
        get
        {
            lock (_gate)
            {
                return _next;
            }
        }
    }

    /// <summary>Selects the cadence and recomputes the next boundary from now.</summary>
    public void Configure(AnnouncementInterval interval, DateTimeOffset? anchor45)
    {
        lock (_gate)
        {
            _interval = interval;
            _anchor45 = anchor45;
            _next = _calculator.NextAfter(_clock.Now, interval, anchor45);
        }
    }

    /// <summary>
    /// Recomputes the next boundary from the current time. Used after clock,
    /// timezone, DST or resume events. Does not replay anything missed.
    /// </summary>
    public void Reschedule()
    {
        lock (_gate)
        {
            _next = _calculator.NextAfter(_clock.Now, _interval, _anchor45);
        }
    }

    /// <summary>
    /// Consumes the pending boundary if it is due right now. Stale boundaries
    /// are skipped silently. Repeated calls return false until a new boundary.
    /// </summary>
    public bool TryConsumeDue(out DateTimeOffset dueAt)
    {
        lock (_gate)
        {
            dueAt = default;
            var now = _clock.Now;

            for (var guard = 0; guard < MaxSkipIterations && _next is { } next && now >= next; guard++)
            {
                _next = _calculator.NextAfter(next, _interval, _anchor45);

                if (now - next <= MissedTolerance)
                {
                    dueAt = next;
                    return true;
                }

                now = _clock.Now;
            }

            return false;
        }
    }
}
