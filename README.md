# SoliloClock

A small, offline Windows tray talking clock. SoliloClock announces the time on a
15, 30, 45 or 60 minute cadence, sometimes following it with an original
`fortune`-style line. It is a native C#/WPF utility: no browser runtime, no
account, no telemetry, no network access.

![SoliloClock in the Pink theme, showing the analog clock, the time 8:28 PM, and the 15/30/45/60 interval buttons](doc/screenshot.png)

The project plan lives in [`doc/plan.md`](doc/plan.md) and current progress is
tracked in [`STATUS.md`](STATUS.md).

## Status

The M0 skeleton and M1 "minimum lovable product" are implemented:

- Animated vector analog clock and digital readout.
- Four interval buttons: **15**, **30**, **45**, **60**.
- Windows SAPI speech, **Speak now**, mute, and an English announcement formatter.
- Wall-clock scheduling with explicit anchored 45-minute semantics; missed
  announcements after sleep/resume are skipped rather than replayed.
- Five bundled themes: Pink, Blue, Black, Neon, Transparent.
- Tray icon with interval, mute, theme, options, show and exit commands; close
  hides to tray with a one-time explanation.
- Single-instance behavior; a second launch reveals the existing clock.
- 100-line English phrase catalog at a 25% joke rate with no repeats in the last
  20 selections.
- Settings persisted atomically to `%APPDATA%\SoliloClock\settings.json`.
- **M2.1 localization foundation:** per-locale UI string catalogs, an independent
  speech language, a locale-keyed time-formatter factory, an installed-voice picker
  with **Test voice**, RTL/language plumbing, and grapheme-safe tray text.

Not yet implemented: the M2.2–M2.6 language packs (French, German, Spanish, Italian,
Dutch, Portuguese first, then the remaining regions), quiet hours, usage reminders,
tone/category settings UI, diagnostics page, installer (M3/M4).

## Requirements

- Windows 10 or Windows 11.
- .NET 10 SDK or newer (built with 11.0.100) and the .NET 10 Desktop Runtime for
  a framework-dependent build.
- A Windows speech voice for audio. The clock still runs and shows captions when
  no voice is installed.

> **Support-lifecycle note:** the project targets `net10.0-windows`, the current
> LTS (supported to November 2028). It was retargeted from `net9.0-windows` after
> .NET 9 left support in May 2026; only the TFM and the `System.Speech` package
> version changed, not the code.

## Build, run, test

```powershell
dotnet build SoliloClock.sln -c Release
dotnet run --project src/SoliloClock.App
dotnet test test/SoliloClock.Tests/SoliloClock.Tests.csproj
```

or use the helper scripts:

```powershell
pwsh tools/build.ps1          # builds Release, then runs the tests
pwsh tools/publish.ps1        # self-contained win-x64 single file in artifacts/
pwsh tools/publish.ps1 -FrameworkDependent
```

The publish script produces `artifacts/<runtime>/SoliloClock.exe`. A
self-contained build needs the matching runtime pack to be downloadable the
first time.

## Using SoliloClock

- **Intervals** are cadences, not polling loops. **15** announces at `:00`,
  `:15`, `:30`, `:45`; **30** at `:00` and `:30`; **60** on the hour.
- **45** announces every 45 elapsed minutes from the moment it was selected
  (for example 10:07 → 10:52 → 11:37). The anchor is kept across restarts.
- Only one announcement fires per boundary. Advancing the clock, changing
  timezone/DST, resuming from sleep or unlocking reschedules from *now*; a
  boundary missed by more than 90 seconds is dropped, not replayed.
- **Mute** silences scheduled announcements only. The clock keeps animating, and
  **Speak now** is always explicit and still speaks.
- **Speak now** speaks the current time immediately and may include a line from
  the phrase catalog, per the joke rate.
- **Close** hides the window to the tray and explains this the first time.
  **Exit** is in the tray menu. Left-clicking the tray icon shows the clock.
- The theme chip cycles all five themes; the tray menu also lists them. In the
  **Transparent** theme an opacity slider appears. **Always on top** is the pin
  button (also in tray Options).
- **Reduced motion** is in tray Options. It is also forced on when Windows
  reports animations are disabled. When hidden, the clock stops rendering
  entirely; announcements continue.
- Right-click the tray icon for the next scheduled announcement. Tray tooltip
  shows the current theme and next time.

## Themes

| Theme | Personality |
|---|---|
| Pink | Rose and lilac card, soft sparkles and hearts. |
| Blue | Cobalt/sky-blue card, sporty hands. |
| Black | Near-black graphite card, steel typography. |
| Neon | Dark navy with pulsing cyan/magenta glow. |
| Transparent | Translucent floating clock with adjustable opacity. |

Themes only change appearance; they do not affect voice or language.

## Speech and languages

Speech uses Windows SAPI through `System.Speech`. The voice list is whatever
Windows actually has installed, shown with the culture each voice speaks;
`Options → Voice` offers **Test voice** and states outright when no installed
voice can speak the selected language. The app never claims a language can speak
without a compatible voice.

The UI and announcements are English (`en-GB`) today. Localization is delivered
in language stages (see `doc/plan.md` §14): the M2.1 foundation is in place, so a
language ships only once a human-authored UI catalog, its own time grammar with
tests, and an adapted phrase catalog exist. The other 15 languages are
intentionally not stubbed with machine-translated jokes.

UI language and announcement language are separate settings
(`UiLanguage` / `SpeechLanguage`); both live in `settings.json` and are validated
on load. A locale without bundled strings or grammar falls back to English and
says so in the status line instead of mixing languages silently.

If no voice is available, the app still starts, shows the last spoken text as a
caption, and explains the situation in the status line and a tray balloon.

## Phrase catalog

`src/SoliloClock.App/Resources/Phrases/en-GB.json` contains 100 newly written
lines. Fields per entry: `id`, `text`, `category`, `tone`, `weight`,
`familyFriendly`, `minActiveMinutes`, `timeWindow`.

Selection:

- Time is always announced first; a joke is an optional extra sentence.
- Default 25% joke rate (0/10/25/50/100 supported by settings).
- Weighted random selection, no repeat of a phrase in the last 20 picks.
- Tone presets: polite (polite lines only), cheeky (polite + cheeky), unhinged
  (all). Default cheeky.
- Day-part windows keep morning/afternoon/evening/late-night lines in context.
- `minActiveMinutes` is gated on app-observed elapsed session time since launch,
  not on invented computer usage. v1 has one three-hour break line.
- Invalid or duplicate entries are skipped with diagnostics; the rest load.
- The family-friendly flag is honoured; all bundled lines currently pass it.

## Settings

`%APPDATA%\SoliloClock\settings.json`, written atomically via a temp file and
replace. A malformed file is backed up as `settings.invalid-<timestamp>.json`
and defaults are used. Values are validated and clamped on load. The app never
needs administrator rights and writes nowhere else.

## Repository layout

```text
SoliloClock.sln
src/SoliloClock.App/       WPF app, themes, phrase resources, tray, services
test/SoliloClock.Tests/    xUnit tests for scheduler, settings, phrases, formatting
tools/                     build and publish scripts
doc/plan.md                Product specification
```
