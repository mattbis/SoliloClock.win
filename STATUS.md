# STATUS — SoliloClock

**Last updated:** 2026-11-21
**Stage:** M0 complete. M1 implemented and verified at build/test level. M2–M4 not started.
**Name:** SoliloClock (per user request; the plan's working name was "Talking Clock").

## Verification in this run

| Check | Result |
|---|---|
| `dotnet build SoliloClock.sln -c Release` | 0 warnings, 0 errors |
| `dotnet test test/SoliloClock.Tests/SoliloClock.Tests.csproj -c Release` | **54 passed**, 0 failed, 0 skipped |
| Release executable startup | Process stayed alive through a 7-second smoke check with the tray icon created |
| Single instance | Second launch exited with code 0 while the first process stayed running |
| Framework-dependent publish | `artifacts/win-x64/SoliloClock.exe` (single file, 931 KB) plus `Resources/Phrases/en-GB.json` |
| Theme dictionary keys | All five themes expose the identical 25 resource keys; all 24 `DynamicResource` references resolve |
| SAPI on this machine | 5 voices installed: Microsoft Hazel Desktop, Microsoft Hazel, Microsoft Susan, Microsoft George (en-GB), Microsoft Zira Desktop (en-US) |

**Not verified:** audible speech output, visual inspection of all five themes,
transparency/DPI/multi-monitor behavior, screen-reader output, live tray-menu
clicks, a real 15-minute elapsed announcement, and hidden-state CPU/RAM
measurement. The render loop is detached while hidden and only a single-shot
announcement timer remains, but no profiler measurement was taken.

## Completed — M0 skeleton

- [x] WPF project builds and launches with no console window.
- [x] Tray icon with Show/Hide/Exit, interval, mute, theme, options.
- [x] Close-to-tray with a one-time explanation and Exit in the tray menu.
- [x] Single-instance behavior; second launch signals the existing instance to
      reveal itself.
- [x] Atomic JSON settings persistence and malformed-file recovery.
- [x] `.editorconfig`, `.gitignore`, README, LICENSE, build/publish scripts, solution.

## Completed — M1 minimum lovable product

- [x] Animated vector analog clock with smooth sweep second hand, digital readout.
- [x] Exactly four interval buttons: 15, 30, 45, 60, with clear active state.
- [x] Windows SAPI speech; Speak Now and Mute work independently of the clock.
- [x] Accurate wall-clock scheduling and explicit anchored 45-minute semantics.
- [x] Missed announcements are skipped (90-second tolerance), never replayed.
- [x] Five themes, including a functional Transparent theme with opacity slider
      and always-on-top toggle.
- [x] Tray-only operation; clock rendering stops while hidden.
- [x] 100 English phrases, 25% joke rate, weighted selection, no repeat in last 20.
- [x] Tone, family-friendly, day-part and session-age filters in the engine.
- [x] No Python, Electron, login, cloud service, network access or admin requirement.
- [x] xUnit coverage for scheduling, DST, settings, catalogs, selection and formatting.

## Defaults and deviations chosen

- **TFM:** `net9.0-windows` because only .NET 8/9 SDKs are installed (9.0.205 is
  latest) and no .NET 10 SDK is present. .NET 9 is out of support as of
  May 2026; retargeting to `net10.0-windows` after installing the .NET 10 SDK is
  the first follow-up. See README for details.
- **Speech:** `System.Speech` 9.0 SAPI adapter. Announcements are cancelled
  before a new one starts, so stale/long speech cannot stack.
- **45 minutes:** elapsed cadence anchored to the moment 45 was selected,
  persisted as `Anchor45` across restarts. Settings/tray compute the next one.
- **Tolerance:** a boundary may fire up to 90 seconds late; older boundaries are
  skipped after sleep/resume/DST/clock changes.
- **Natural time:** default English natural phrasing ("It's quarter past seven").
  A 24-hour preference speaks exact 24-hour time ("It's 19:45").
- **Phrases:** all 100 plan seeds are original and included. The only
  `minActiveMinutes` gate uses app-observed elapsed session time since launch
  (not invented computer usage).
- **Settings UI:** M1 exposes theme cycling, pin, opacity, mute and reduced motion
  (tray Options). A full settings page, joke-rate/tone/category UI and voice
  picker remain future work.
- **Transparency:** translucent card plus adjustable window opacity; click-through
  is deliberately not enabled (it is optional in the plan).
- **Languages:** v1 is English-only by decision, not by omission. The plan's
  16-language table stays as the M2 roadmap; shipping a language needs a
  human-authored catalog, locale-specific time grammar and a compatible
  installed Windows voice. Locale detection is deliberately not wired up yet:
  with only `en-GB` bundled it would always fall back to English and surface a
  "no catalog for <locale>" status line, which is worse than plain English.
- **First close:** one-time informational MessageBox explaining the tray.

## Known limitations

- English-only UI and announcements; no localized time grammar yet (M2).
- Speech language/voice is not selectable in the UI; default/installed voice is
  used. Voice availability is reported honestly when absent.
- No quiet hours, usage reminders, 20-20-20 nudges, hotkey, chimes or startup
  automation (M3).
- No diagnostics page or rotating log file yet; status line and tray balloon
  carry the current error.
- No family-friendly toggle UI though the engine supports it; bundled content
  passes the filter.
- High-contrast theme tuning, screen-reader QA, RTL/CJK/Hindi shaping and
  multi-monitor testing remain unverified.
- Transparent theme can look different over light versus dark wallpapers; the
  opacity slider is the intended control.
- Publish script's default self-contained build has not been run; the
  framework-dependent artifact requires the .NET 9 Desktop Runtime.

## Next actionable steps

1. Install the .NET 10 SDK, retarget both projects to `net10.0-windows`, bump
   `System.Speech`, rebuild and rerun the 54 tests.
2. Manual QA pass: listen to Speak Now and a scheduled announcement, inspect all
   five themes at 100/150/200% DPI, exercise the tray menu and close-to-tray.
3. Measure hidden idle CPU/RAM over an hour and confirm no duplicate announcement
   across a sleep/resume cycle.
4. M2: localization infrastructure, per-locale time formatters, real translated
   catalogs, voice picker with Test Voice and unavailable-voice states.
5. M3: opt-in active-session reminders with the documented metric and cooldowns,
   quiet hours, tone/category/family settings, diagnostics page.
6. M4: portable self-contained x64/ARM64 packaging, reproducible release checksums.
