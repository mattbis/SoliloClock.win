using System;

namespace SoliloClock.Services;

/// <summary>
/// Injectable clock so scheduling can be tested without waiting on wall time.
/// </summary>
public interface IClock
{
    DateTimeOffset Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
