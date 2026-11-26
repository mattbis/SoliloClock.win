# STATUS — SoliloClock

**Last updated:** 2026-11-26
**Stage:** M0 and M1 complete. **M2.1 localization foundation complete, including automatic Windows-language detection and the live language picker. M2.2 French (`fr-FR`) and German (`de-DE`) complete; Welsh (`cy-GB`) ships as a provisional draft pending native review.** Spanish, Italian, Dutch, Portuguese and M2.3–M2.6 not started; M3–M4 not started.
**Name:** SoliloClock.

## Verification in this run

| Check | Result |
|---|---|
| `dotnet build SoliloClock.sln -c Release` | 0 warnings, 0 errors |
| `dotnet test test/SoliloClock.Tests/SoliloClock.Tests.csproj -c Release --no-build` | **238 passed**, 0 failed, 0 skipped |
| Release executable startup | Process stayed alive through a 7-second smoke check; no leftover process |
| Catalog deployment | `en-GB`/`fr-FR`/`de-DE`/`cy-GB` UI catalogs and all four phrase catalogs present in the build output |
| UI string completeness | Reflected test requires every `LocalizationKeys` constant in every shipped catalog |
| French phrasing | 18 natural 12-hour cases, 7 twenty-four-hour and 4 exact-reading cases assert the whole sentence |
| French catalogs | 100 phrases load with zero diagnostics, share the English ids, and keep the reviewed metadata |
| French typography | Every UI string and all 100 phrases are checked for a non-breaking space before `: ; ? !`, non-breaking spaces inside guillemets, balanced « » and typographic apostrophes |
| German phrasing | 24 natural 12-hour cases, 7 twenty-four-hour and 5 exact-reading cases assert whole sentences; every minute in three modes produces a German sentence |
| German grammar edge | "ein Uhr" but "halb eins"/"Viertel vor eins"; compounds use the linking "ein-" ("einundzwanzig") |
| German catalogs | 100 phrases load with zero diagnostics, share the English ids and keep the reviewed metadata; „…“ quotes, no straight apostrophes or double spaces |
| German selection | `de-DE` is shipped and not provisional; `de-AT` inherits the grammar and a German Windows resolves to `de-DE` automatically |
| Automatic language | 13 resolver cases: shipped match, `fr-CA`→`fr-FR`, unsupported→English, unknown tag, explicit override |
| Automatic voice | 8 catalog cases: language match, region preference, alphabetical tie-break, no matching voice, blank cultures, empty voice list, unset locale |
| Welsh phrasing | 19 natural 12-hour cases, 6 twenty-four-hour and 3 exact-reading cases assert whole sentences; all 1,440 clock minutes produce a Welsh sentence |
| Welsh catalogs | 100 phrases load with zero diagnostics, share the English ids and keep the reviewed metadata |
| Provisional flag | `cy-GB` is the only provisional locale, and a Welsh Windows resolves to it automatically |
| Live language switching | 4 view-model cases drive the real `MainViewModel` through UI and announcement language changes and assert chrome, caption, status, culture, grammar and voice follow at once |

**Not verified:** audible speech in any language (including Test voice, a French SAPI
voice, and whether the automatic voice choice sounds right), visual inspection of the
window or tray menu, actual tray-menu clicks, RTL/CJK rendering, screen-reader output,
and a real timed announcement. No profiler
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
- [x] With no voice pinned, announcements use an installed voice that speaks the
      announcement language (exact region, else any voice of that language, else the
      Windows default), re-resolved on each announcement so newly installed voices are
      picked up. Passing the resolved name explicitly also fixes *Automatic* previously
      keeping whichever voice SAPI had last selected.
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

## Completed — Welsh (`cy-GB`), provisional

- [x] `Resources/Localization/cy-GB.json` covers every key; `Resources/Phrases/cy-GB.json`
      holds 100 lines with the same ids, categories, tones, weights, windows and
      active-minute gates as `en-GB` (asserted by test).
- [x] `WelshTimeFormatter`: *o'r gloch*, *chwarter wedi*, *hanner awr wedi*,
      *chwarter i*, soft mutation after *yn* and *i* ("dri o'r gloch", "bum munud",
      "ddeng munud i ddeg"), aspirate mutation after *a*, and named *hanner dydd* /
      *hanner nos*.
- [x] `cy-GB` ships with `LocaleDefinition.IsProvisional`, so both language menus
      show *Cymraeg (drafft — aros adolygiad brodorol)*; automatic resolution selects
      it on a Welsh Windows without any setup.
- [ ] **Outstanding:** every string, phrase and the time grammar is machine-drafted
      and needs a Welsh speaker's review before the flag is cleared.

## Completed — M2.2 German (`de-DE`)

- [x] `Resources/Localization/de-DE.json` covers every key in the formal *Sie* form,
      with German ß and „…“ quotation marks (asserted by test).
- [x] `GermanTimeFormatter`: *Es ist neun Uhr*, *Viertel nach neun*, *halb zehn*
      (halfway to ten), *Viertel vor zehn*, *ein Uhr* but *halb eins*,
      *Mittag*/*Mitternacht*, and a literal 24-hour reading
      (*neunzehn Uhr fünfundvierzig*). Minutes between the quarters are read as clock
      time (*neun Uhr zwanzig*), avoiding the regionally split *zwanzig nach neun*.
- [x] `Resources/Phrases/de-DE.json`: 100 translations under the same ids, categories,
      tones, weights, windows and active-minute gates as `en-GB`.
- [x] `de-DE` promoted in `LocaleRegistry`; `de-AT` and other German locales inherit the
      grammar through the language match.
- [ ] **Outstanding:** voice check on a machine with an installed German voice — the
      German text path is unit-tested only in this environment.

## Language behaviour

| Setting | `null` / `auto` | explicit tag |
|---|---|---|
| `UiLanguage` | Windows display language, mapped to the best shipped catalog | that catalog; a missing one falls back to English and is reported |
| `SpeechLanguage` | the interface language | that language's grammar; missing grammar is announced in English and reported |

Switching either one re-localizes immediately as far as the catalogs allow: window
chrome, tooltips, tray menu, caption, status, next-announcement text, theme name,
culture/flow direction, phrase catalog and formatter. A voice that cannot speak the
new language is called out in the status line and in the Voice menu. This path is
covered by `LiveLanguageSwitchingTests`, which switches the real view model and asserts
what the user then sees and hears.

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

- `en-GB`, `fr-FR`, `de-DE` and provisional `cy-GB` ship; the other thirteen planned
  locales stay unshipped.
- Welsh content is a draft, not a translation anyone has checked: it is marked in both
  language menus and must be replaced after native review. Its time grammar is only as
  good as the draft wording, particularly mutations and the 24-hour reading.
- Neither French nor German speech was audible in this environment, so pronunciation
  and both "no installed voice speaks …" paths are unit-tested only. German SAPI voices
  (for example Microsoft Hedda) ship with some Windows installs, but none is installed
  here.
- The tray language/voice menus are rebuilt from view-model state but were not clicked
  through; the picker's behaviour is covered by resolver, settings and catalog tests,
  not by UI automation.
- `PhraseCatalog`/`LocalizationCatalog` diagnostics and Windows/SAPI `ex.Message` text
  remain English/OS-language by design.
- The Voice menu lists every installed voice in name order, without grouping the ones
  that speak the announcement language, and a pinned voice that cannot speak it is not
  flagged in the menu.
- Speech rate/volume, joke rate and tone still have no settings UI.
- RTL, CJK, Devanagari shaping, high-contrast, screen-reader output and multi-monitor
  behavior remain visually unverified.
- No diagnostics page or rotating log file yet.

## Next actionable steps

1. **Native review of Welsh:** replace the draft UI strings, 100 phrases and time
   grammar, then clear `IsProvisional` for `cy-GB`.
2. **Spanish (`es-ES`)**: catalog, `SpanishTimeFormatter` ("son las nueve y cuarto",
   "menos cuarto", inverted punctuation in questions), 100 phrases, promotion.
3. Italian, Dutch and Portuguese to finish M2.2.
4. Then M2.3 (Polish, Russian, Greek), M2.4 (Arabic RTL), M2.5 (Hindi), M2.6 (CJK).
5. M3: opt-in active-session reminders, quiet hours, tone/category settings, diagnostics.
