# STATUS — SoliloClock

**Last updated:** 2026-11-26
**Stage:** M0 and M1 complete. **M2.1 localization foundation complete, including automatic Windows-language detection and the live language picker. M2.2 French (`fr-FR`) complete.** Other Western European languages and M2.3–M2.6 not started; M3–M4 not started.
**Name:** SoliloClock.

## Verification in this run

| Check | Result |
|---|---|
| `dotnet build SoliloClock.sln -c Release` | 0 warnings, 0 errors |
| `dotnet test test/SoliloClock.Tests/SoliloClock.Tests.csproj -c Release --no-build` | **142 passed**, 0 failed, 0 skipped |
| Release executable startup | Process stayed alive through a 7-second smoke check; no leftover process |
| Catalog deployment | `en-GB`/`fr-FR` UI catalogs and both phrase catalogs present in the build output |
| UI string completeness | Reflected test requires every `LocalizationKeys` constant in every shipped catalog |
| French phrasing | 18 natural 12-hour cases, 7 twenty-four-hour and 4 exact-reading cases assert the whole sentence |
| French catalogs | 100 phrases load with zero diagnostics, share the English ids, and keep the reviewed metadata |
| Automatic language | 13 resolver cases: shipped match, `fr-CA`→`fr-FR`, unsupported→English, unknown tag, explicit override |

**Not verified:** audible speech in any language (including Test voice and a French
SAPI voice), visual inspection of the window or tray menu, actual tray-menu clicks,
RTL/CJK rendering, screen-reader output, and a real timed announcement. No profiler
measurement was taken.

## Completed — M0 skeleton and M1 minimum lovable product

- [x] WPF tray app, single instance, close-to-tray, Show/Hide/Exit, atomic settings.
- [x] Animated vector analog clock, digital readout, four interval buttons (15/30/45/60).
- [x] Windows SAPI speech, Speak Now, mute, wall-clock scheduling, missed-announcement skip.
- [x] Five themes including a functional Transparent theme.
- [x] 100 English phrases, 25% joke rate, weighted selection, no repeat in last 20.
- [x] xUnit coverage for scheduling, DST, settings, catalogs, selection and formatting.

## Completed — M2.1 localization foundation

- [x] **Language is automatic by default.** `UiLanguage` is null (or `auto`/`system`/
      blank) and the Windows display language selects the best shipped catalog:
      exact tag, else a shipped locale speaking the same language (`fr-CA` → `fr-FR`),
      else English with a status-line note. No first-run setup.
- [x] **Explicit override, applied live.** Tray **Options → Language** offers
      *Automatic (Windows language)* plus each shipped language; **Options → Announce in**
      sets the speech language independently. No restart, no settings editing.
- [x] UI language, speech language and voice stay independent; unknown tags repair to
      automatic rather than pinning English.
- [x] Per-locale JSON catalogs layered over the English base, so a partial translation
      keeps reviewed English wording.
- [x] `TimeFormatterFactory` selects grammar by locale, exact tag first then language.
- [x] Installed voices listed with the culture they speak, **Test voice**, and an
      explicit "no installed voice speaks <language>" state.
- [x] Window `FlowDirection`/`Language`, clock numerals and thread culture follow the
      locale; Gregorian wall time and Western digits are forced.
- [x] Grapheme-safe tray truncation; NotifyIcon text capped at its real 63 characters.
- [x] Product naming is SoliloClock everywhere.
- [x] Settings-recovery wording moved from the loader into the view model.

## Completed — M2.2 French

- [x] `Resources/Localization/fr-FR.json`: human-authored catalog covering every key
      (a reflected test asserts completeness), with non-breaking spaces before
      `: ; ? !`, spaced guillemets and typographic apostrophes. A test enforces
      those conventions.
- [x] `FrenchTimeFormatter`: "il est neuf heures et quart", "dix heures moins le quart",
      "midi et demi", "minuit", feminine agreement, day-part suffixes only when the
      12-hour reading is ambiguous, exact 24-hour reading, exact non-natural reading.
- [x] `Resources/Phrases/fr-FR.json`: 100 human-written French lines with the same
      stable ids, categories, tones, weights, windows and active-minute gates as
      `en-GB`; a test fails if a line is still the English text.
- [x] `fr-FR` marked shipped; `fr-CA`/`fr-BE` inherit French grammar by language match.

## Language behaviour

| Setting | `null` / `auto` | explicit tag |
|---|---|---|
| `UiLanguage` | Windows display language, mapped to the best shipped catalog | that catalog; a missing one falls back to English and is reported |
| `SpeechLanguage` | the interface language | that language's grammar; missing grammar is announced in English and reported |

Switching either one re-localizes immediately as far as the catalogs allow: window
chrome, tooltips, tray menu, caption, status, next-announcement text, theme name,
culture/flow direction, phrase catalog and formatter. A voice that cannot speak the
new language is called out in the status line and in the Voice menu.

## Defaults and deviations chosen

- **TFM:** `net10.0-windows`, built with the .NET 10.0.404 SDK.
- **Speech:** `System.Speech` 9.0 SAPI adapter; announcements cancel stale speech first.
- **45 minutes:** elapsed cadence anchored to selection, persisted as `Anchor45`.
- **Tolerance:** a boundary may fire up to 90 seconds late; older boundaries are skipped.
- **Localization storage:** UTF-8 JSON catalogs rather than `.resx` satellite assemblies.
- **Clock calendar:** cultures are cloned to Gregorian plus Western digits when needed.
- **Settings recovery is reported in English in practice,** because an unreadable
  settings file also takes its `UiLanguage` with it; recovered defaults select automatic,
  which is English on an English system. The wording lives in the catalog so it localizes
  as soon as a recoverable case keeps the language.
- **Speech language resets when the interface changes** only if it was following the
  interface; an explicit speech choice survives a UI language change.

## Known limitations

- Only `en-GB` and `fr-FR` ship; the other fourteen planned locales stay unshipped.
- No French voice was audible in this environment, so pronunciation and the
  "no installed voice speaks Français" path are unit-tested only.
- The tray language/voice menus are rebuilt from view-model state but were not clicked
  through; the picker's behaviour is covered by resolver, settings and catalog tests,
  not by UI automation.
- `PhraseCatalog`/`LocalizationCatalog` diagnostics and Windows/SAPI `ex.Message` text
  remain English/OS-language by design.
- The Voice menu lists every installed voice and the language warning; no per-language
  filter yet.
- Speech rate/volume, joke rate and tone still have no settings UI.
- RTL, CJK, Devanagari shaping, high-contrast, screen-reader output and multi-monitor
  behavior remain visually unverified.
- No diagnostics page or rotating log file yet.

## Next actionable steps

1. **German (`de-DE`)**: catalog, `GermanTimeFormatter` (halb, Viertel nach/vor,
   "siebzehn Uhr fünfundvierzig"), 100 phrases, promotion in `LocaleRegistry`.
2. Add a view-model-level test for live language switching (chrome, status, formatter).
3. Spanish, Italian, Dutch and Portuguese to finish M2.2.
4. Then M2.3 (Polish, Russian, Greek), M2.4 (Arabic RTL), M2.5 (Hindi), M2.6 (CJK).
5. M3: opt-in active-session reminders, quiet hours, tone/category settings, diagnostics.
