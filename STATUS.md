# STATUS — SoliloClock

**Last updated:** 2026-11-26
**Stage:** M0 and M1 complete. **M2.1 (localization foundation) implemented and verified.** M2.2–M2.6 (language stages) and M3–M4 not started.
**Name:** SoliloClock (the plan's working name was "Talking Clock").

## Verification in this run

| Check | Result |
|---|---|
| `dotnet build SoliloClock.sln -c Release` | 0 warnings, 0 errors |
| `dotnet test test/SoliloClock.Tests/SoliloClock.Tests.csproj -c Release --no-build` | **84 passed**, 0 failed, 0 skipped |
| Release executable startup | Process stayed alive through a 7-second smoke check; no leftover process |
| UI catalog deployment | `Resources/Localization/en-GB.json` copied to the build output |
| UI string completeness | Test reflects over every `LocalizationKeys` constant and fails on a missing or empty key |
| RTL plumbing | Unit test: `ar` produces `FlowDirection.RightToLeft`; `ar-SA` keeps a Gregorian calendar |

**Not verified:** audible speech output (including Test voice), visual inspection of the
window or tray menu, actual RTL/CJK rendering, screen-reader output, live tray-menu
clicks, and a real 15-minute announcement. No profiler measurement was taken.

## Completed — M0 skeleton and M1 minimum lovable product

- [x] WPF tray app, single instance, close-to-tray, Show/Hide/Exit, atomic settings.
- [x] Animated vector analog clock, digital readout, four interval buttons (15/30/45/60).
- [x] Windows SAPI speech, Speak Now, mute, wall-clock scheduling, missed-announcement skip.
- [x] Five themes including a functional Transparent theme.
- [x] 100 English phrases, 25% joke rate, weighted selection, no repeat in last 20.
- [x] xUnit coverage for scheduling, DST, settings, catalogs, selection and formatting.

## Completed — M2.1 localization foundation

- [x] **UI language is consumed, not just stored.** `AppSettings.UiLanguage` selects a
  JSON catalog from `Resources/Localization/`; English is always loaded first and the
  requested locale is layered on top, so a partial translation keeps reviewed English
  wording instead of blanks.
- [x] **Separate UI language, speech language and voice.** New `SpeechLanguage` setting
  defaults to the UI language; `SpeechVoice` stays independent. Invalid tags repair to
  `en-GB` on load.
- [x] **Time formatter no longer hardcoded.** `TimeFormatterFactory` resolves grammar by
  locale (`en-GB`, `en-US`, `en-AU`, neutral `en`) and falls back to English with an
  explicit diagnostic. `MainViewModel` receives the selection instead of `new()`ing English.
- [x] **Voice picker with real data.** Installed voices are exposed as `VoiceOption`
  (name + BCP-47 culture); the tray `Options → Voice` menu lists only installed voices,
  shows the culture, offers **Test voice**, and states "No installed voice speaks
  <language>" when the language cannot be spoken.
- [x] **Honest startup status** for a missing UI translation, missing time grammar,
  missing voice, no voices at all, and no phrase catalog.
- [x] **RTL and culture plumbing.** Window `FlowDirection` and `Language` follow the
  locale; the analog clock numerals use the inherited culture and flow direction; the
  clock forces Gregorian wall time and Western digits even for `ar-SA`.
- [x] **Grapheme-safe truncation** (`TextTruncation`) replaces UTF-16 slicing, so tray
  tooltips never split surrogate pairs, emoji or combining marks. NotifyIcon text is
  capped at its real 63-character limit.
- [x] **Product naming fixed.** Short status, tray tooltip, close-to-tray dialog, window
  title and tray listener thread all say SoliloClock.
- [x] **Tests added (30 new):** catalog completeness and fallback, `UiText` key behavior,
  RTL direction, Gregorian calendar under `ar-SA`, locale registry integrity, tag
  normalisation, formatter selection, grapheme truncation, language normalization in settings.

## Defaults and deviations chosen

- **TFM:** `net10.0-windows` (LTS, supported to November 2028), built with the .NET 10.0.404 SDK.
- **Speech:** `System.Speech` 9.0 SAPI adapter; announcements cancel stale speech first.
- **45 minutes:** elapsed cadence anchored to selection, persisted as `Anchor45`.
- **Tolerance:** a boundary may fire up to 90 seconds late; older boundaries are skipped.
- **Localization storage:** UTF-8 JSON catalogs (`Resources/Localization/<locale>.json`,
  phrase catalogs in `Resources/Phrases/`) rather than `.resx` satellite assemblies. This
  keeps partial translation overlays, per-key fallback and testability simple;
  `SatelliteResourceLanguages=en` therefore only affects NuGet assets.
- **Clock calendar:** the culture is cloned to Gregorian plus Western digits when a locale
  would otherwise show another calendar (for example `ar-SA` Um al-Qura). A wall clock
  should not change eras with the UI language.
- **Language changes apply on restart.** There is no language picker in the UI yet
  (settings file only), and no live re-localization is attempted.
- **Fallback is stated, never silent:** a locale without strings or grammar reports it in
  the status line rather than mixing languages quietly.

## Known limitations

- Only `en-GB` ships. `LocaleRegistry` lists the planned locales as *not shipped*; adding
  one requires a human-authored catalog, a locale formatter with tests, an adapted phrase
  catalog and a voice-availability statement.
- Settings-recovery messages from `SettingsService` remain English diagnostics (unlike
  status-line text, which is localized).
- The Voice menu lists every installed voice and the language warning, but there is no
  per-language filter yet.
- Speech language, speech rate/volume, joke rate and tone still have no settings UI.
- RTL, CJK, Devanagari shaping, high-contrast, screen-reader output and multi-monitor
  behavior remain visually unverified.
- No diagnostics page or rotating log file yet.

## Next actionable steps

1. **M2.2 — French first** (`fr-FR`): human-authored UI catalog, `FrenchTimeFormatter`
   with quarter/half/midnight/noon tests, adapted phrase catalog, and a voice note.
   Promoting `fr-FR` in `LocaleRegistry` is the stage gate.
2. Repeat M2.2 for German, Spanish, Italian, Dutch and Portuguese.
3. Then M2.3 (Polish, Russian, Greek), M2.4 (Arabic RTL), M2.5 (Hindi), M2.6 (CJK).
4. Small follow-ups: localize settings-recovery diagnostics, add a language picker,
   and filter the voice list by announcement language.
5. M3: opt-in active-session reminders, quiet hours, tone/category settings, diagnostics.
