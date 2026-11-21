using System;
using System.Collections.Generic;

namespace SoliloClock.Models;

public enum PhraseCategory
{
    Computer,
    Break,
    LateNight,
    Morning,
    Afternoon,
    Evening,
    Fortune,
    Rare,
    Seasonal,
    User,
}

public enum DayPart
{
    Morning,
    Afternoon,
    Evening,
    LateNight,
}

public static class PhraseCategories
{
    public static bool TryParse(string? value, out PhraseCategory category)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "computer":
                category = PhraseCategory.Computer;
                return true;
            case "break":
                category = PhraseCategory.Break;
                return true;
            case "late-night":
                category = PhraseCategory.LateNight;
                return true;
            case "morning":
                category = PhraseCategory.Morning;
                return true;
            case "afternoon":
                category = PhraseCategory.Afternoon;
                return true;
            case "evening":
                category = PhraseCategory.Evening;
                return true;
            case "fortune":
                category = PhraseCategory.Fortune;
                return true;
            case "rare":
                category = PhraseCategory.Rare;
                return true;
            case "seasonal":
                category = PhraseCategory.Seasonal;
                return true;
            case "user":
                category = PhraseCategory.User;
                return true;
            default:
                category = default;
                return false;
        }
    }

    public static bool TryParseDayPart(string? value, out DayPart dayPart)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "morning":
                dayPart = DayPart.Morning;
                return true;
            case "afternoon":
                dayPart = DayPart.Afternoon;
                return true;
            case "evening":
                dayPart = DayPart.Evening;
                return true;
            case "late-night":
                dayPart = DayPart.LateNight;
                return true;
            default:
                dayPart = default;
                return false;
        }
    }
}

/// <summary>
/// One catalog line. <see cref="Weight"/> is a relative pick weight;
/// <see cref="MinActiveMinutes"/> gates lines on app-observed session age.
/// </summary>
public sealed record Phrase(
    string Id,
    string Text,
    PhraseCategory Category,
    ContentTone Tone,
    int Weight,
    DayPart? TimeWindow,
    int? MinActiveMinutes,
    bool FamilyFriendly);

/// <summary>
/// Immutable input for a single phrase selection.
/// </summary>
public sealed record PhraseRequest(
    ContentTone MaxTone,
    bool FamilyFriendly,
    IReadOnlySet<PhraseCategory> EnabledCategories,
    DateTime LocalNow,
    int SessionMinutes);
