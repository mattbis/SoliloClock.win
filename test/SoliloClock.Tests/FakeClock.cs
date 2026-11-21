using System;
using SoliloClock.Services;

namespace SoliloClock.Tests;

internal sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset now)
    {
        Now = now;
    }

    public DateTimeOffset Now { get; set; }
}
