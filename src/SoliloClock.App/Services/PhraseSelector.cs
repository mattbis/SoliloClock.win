using System;
using System.Collections.Generic;
using System.Linq;
using SoliloClock.Models;

namespace SoliloClock.Services;

/// <summary>
/// Picks a phrase with weighted randomness while avoiding recent repeats.
/// Selection is deterministic when a seeded <see cref="Random"/> is supplied.
/// </summary>
public sealed class PhraseSelector
{
    private readonly List<Phrase> _phrases;
    private readonly Random _random;
    private readonly int _historyLimit;
    private readonly LinkedList<string> _history = new();
    private readonly HashSet<string> _recentIds = new(StringComparer.OrdinalIgnoreCase);

    public PhraseSelector(
        IEnumerable<Phrase> phrases,
        Random? random = null,
        int historyLimit = 20)
    {
        _phrases = phrases?.ToList() ?? throw new ArgumentNullException(nameof(phrases));
        _random = random ?? new Random();
        _historyLimit = Math.Max(0, historyLimit);
    }

    public int CatalogSize => _phrases.Count;

    public IReadOnlyCollection<string> RecentIds => _recentIds;

    public Phrase? Select(PhraseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var eligible = _phrases.Where(phrase => IsEligible(phrase, request)).ToList();
        if (eligible.Count == 0)
        {
            return null;
        }

        var fresh = eligible.Where(phrase => !_recentIds.Contains(phrase.Id)).ToList();
        var pool = fresh.Count > 0 ? fresh : eligible;

        var totalWeight = 0L;
        foreach (var phrase in pool)
        {
            totalWeight += phrase.Weight;
        }

        var chosen = pool[^1];
        if (totalWeight > 0)
        {
            var roll = _random.NextDouble() * totalWeight;
            foreach (var phrase in pool)
            {
                roll -= phrase.Weight;
                if (roll < 0)
                {
                    chosen = phrase;
                    break;
                }
            }
        }

        Remember(chosen.Id);
        return chosen;
    }

    private void Remember(string id)
    {
        if (_historyLimit == 0)
        {
            return;
        }

        _history.AddLast(id);
        _recentIds.Add(id);

        while (_history.Count > _historyLimit)
        {
            _recentIds.Remove(_history.First!.Value);
            _history.RemoveFirst();
        }
    }

    private static bool IsEligible(Phrase phrase, PhraseRequest request)
    {
        if (phrase.Tone > request.MaxTone)
        {
            return false;
        }

        if (request.FamilyFriendly && !phrase.FamilyFriendly)
        {
            return false;
        }

        if (!request.EnabledCategories.Contains(phrase.Category))
        {
            return false;
        }

        if (phrase.MinActiveMinutes is { } minActive && request.SessionMinutes < minActive)
        {
            return false;
        }

        if (phrase.TimeWindow is { } window && !MatchesDayPart(window, request.LocalNow.Hour))
        {
            return false;
        }

        return true;
    }

    private static bool MatchesDayPart(DayPart window, int hour) => window switch
    {
        DayPart.Morning => hour is >= 5 and < 12,
        DayPart.Afternoon => hour is >= 12 and < 18,
        DayPart.Evening => hour is >= 18 and < 23,
        DayPart.LateNight => hour >= 23 || hour < 5,
        _ => true,
    };
}

/// <summary>
/// 0/10/25/50/100 joke-rate decision, kept separate so it can be tested
/// without randomness.
/// </summary>
public static class JokePolicy
{
    public static bool IncludeJoke(int percent, int roll) => percent > 0 && roll < percent;
}
