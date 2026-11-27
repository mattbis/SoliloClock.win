# STATUS — SoliloClock

**Last updated:** 2026-11-26
**Stage:** M0, M1 and the original M2 plan are complete: seventeen locales (M2.1–M2.6), each with a UI catalog, 100 phrases and its own time grammar. **M2.7 worldwide expansion is in progress: it targets every language Windows itself can speak, and wave 1 adds Danish (`da-DK`), Swedish (`sv-SE`), Norwegian Bokmål (`nb-NO`) and Czech (`cs-CZ`) as provisional packs — 21 locales shipped, 15 Windows-voice languages still to add.** Eleven machine-drafted packs carry the draft marker, the tray menu and close-to-tray dialog mirror for right-to-left Arabic, and M3–M4 have not started.
**Name:** SoliloClock.

## Verification in this run

| Check | Result |
|---|---|
| `dotnet build SoliloClock.sln -c Release` | 0 warnings, 0 errors |
| `dotnet test test/SoliloClock.Tests/SoliloClock.Tests.csproj -c Release --no-build` | **565 passed**, 0 failed, 0 skipped |
| Release executable startup | Process stayed alive through a 7-second smoke check; no leftover process |
| Catalog deployment | All 21 UI catalogs (`en-GB`, `fr-FR`, `cy-GB`, `de-DE`, `es-ES`, `it-IT`, `nl-NL`, `pt-PT`, `pl-PL`, `ru-RU`, `el-GR`, `da-DK`, `sv-SE`, `nb-NO`, `cs-CZ`, `ar`, `hi`, `zh-Hans`, `zh-Hant`, `ja-JP`, `ko-KR`) and all 21 phrase catalogs present in the build output |
| M2.7 wave 1 | Danish, Swedish, Norwegian and Czech each ship a 71-key catalog, 100-phrase pack and grammar; phrasing cases, regional inheritance (`sv-FI`, `cs-SK`, `da`, `nb`) and all 1,440 minutes pass |
| UI string completeness | Reflected test requires every `LocalizationKeys` constant in every shipped catalog |
| French phrasing | 18 natural 12-hour cases, 7 twenty-four-hour and 4 exact-reading cases assert the whole sentence |
| French catalogs | 100 phrases load with zero diagnostics, share the English ids, and keep the reviewed metadata |
| French typography | Every UI string and all 100 phrases are checked for a non-breaking space before `: ; ? !`, non-breaking spaces inside guillemets, balanced « » and typographic apostrophes |
| German phrasing | 24 natural 12-hour cases, 7 twenty-four-hour and 5 exact-reading cases assert whole sentences; every minute in three modes produces a German sentence |
| German grammar edge | "ein Uhr" but "halb eins"/"Viertel vor eins"; compounds use the linking "ein-" ("einundzwanzig") |
| German catalogs | 100 phrases load with zero diagnostics, share the English ids and keep the reviewed metadata; „…“ quotes, no straight apostrophes or double spaces |
| German selection | `de-DE` is shipped and not provisional; `de-AT` inherits the grammar and a German Windows resolves to `de-DE` automatically |
| Spanish/Italian phrasing | 18 Spanish and 18 Italian natural cases plus 24-hour and exact readings; every minute in three modes produces a sentence in each language |
| Dutch/Portuguese phrasing | 18 Dutch and 18 Portuguese natural cases plus 24-hour and exact readings; every minute in three modes produces a sentence in each language |
| M2.2 shared acceptance | All six non-English packs load 100 metadata-identical phrases, resolve their own formatter, and are chosen automatically for regional variants (`es-MX`, `it-CH`, `nl-BE`, `pt-BR`) |
| M2.3 shared acceptance | Polish, Russian and Greek each load a complete UI catalog, 100 metadata-identical phrases, resolve their own formatter and are chosen automatically for `pl`, `ru-BY` and `el-CY` |
| Polish phrasing | 14 natural cases (feminine hours, *wpół do* genitive, *za kwadrans*), 3 twenty-four-hour and 1 exact-reading case; Polish plural agreement for *minuta*, *dwie minuty*, *pięć minut* and 21 |
| Russian phrasing | 12 natural cases (genitive hours, *четверть*/*половина*/*без четверти*), 2 twenty-four-hour cases, *час/часа/часов* and *минута/минуты/минут* agreement including 21 and 22 |
| Greek phrasing | 11 natural cases (*και τέταρτο*, *και μισή*, *παρά τέταρτο*, *παρά είκοσι*), named midnight/noon and 2 twenty-four-hour cases |
| M2.3 minute coverage | All 1,440 clock minutes in three modes produce a digit-free sentence in Polish, Russian and Greek |
| M2.4–M2.6 shared acceptance | Arabic, both Chinese scripts and Japanese each load a complete UI catalog, 100 metadata-identical phrases, resolve their own formatter and are registered as provisional |
| Arabic phrasing | 10 natural cases (feminine hours, *والربع*, *والنصف*, *إلا ربعًا*, dual *دقيقتين*, plural *خمس دقائق*, genitive *عشرين* after *إلا*) and a 24-hour case |
| Chinese phrasing | 8 Simplified natural cases (*一刻*/*半*/*差一刻*), a Traditional script check and 24-hour/exact readings; `zh-CN` and `zh-TW` pick independent catalogs |
| Japanese phrasing | 6 natural cases (*半*, *正午*, *零時*), 24-hour and exact readings |
| M2.4–M2.6 minute coverage | All 1,440 clock minutes in three modes produce a digit-free sentence in Arabic, both Chinese scripts and Japanese |
| Automatic language | 13 resolver cases: shipped match, `fr-CA`→`fr-FR`, unsupported→English, unknown tag, explicit override |
| Automatic voice | 8 catalog cases: language match, region preference, alphabetical tie-break, no matching voice, blank cultures, empty voice list, unset locale |
| Welsh phrasing | 19 natural 12-hour cases, 6 twenty-four-hour and 3 exact-reading cases assert whole sentences; all 1,440 clock minutes produce a Welsh sentence |
| Welsh catalogs | 100 phrases load with zero diagnostics, share the English ids and keep the reviewed metadata |
| Hindi phrasing | 9 natural cases (*सवा*, *साढ़े*, *पौने*, *बजकर … मिनट*, *बजने में*), 24-hour and exact readings; all 1,440 clock minutes produce a digit-free sentence |
| Korean phrasing | 8 natural cases (native hours 한/두/세, *반*, Sino minutes), 24-hour and exact readings; all 1,440 clock minutes produce a digit-free sentence |
| RTL handling | Arabic mirrors the WPF window, the WinForms tray menu and the close-to-tray message box; the clock face is pinned to Western digits, asserted for `ar-SA` |
| Provisional flag | The only provisional locales are `cy-GB`, `da-DK`, `sv-SE`, `nb-NO`, `cs-CZ`, `ar`, `hi`, `zh-Hans`, `zh-Hant`, `ja-JP` and `ko-KR`, all shipped with complete packs; a Welsh Windows still resolves to `cy-GB` automatically |
| Live language switching | 4 view-model cases drive the real `MainViewModel` through UI and announcement language changes and assert chrome, caption, status, culture, grammar and voice follow at once |

**Not verified:** audible speech in any language other than English (including Test voice,
the nine non-English announcement languages, and whether the automatic voice choice sounds
right), visual inspection of the window or tray menu, actual tray-menu clicks, RTL/CJK
rendering, screen-reader output, and a real timed announcement. No profiler measurement was
taken.

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

## Completed — M2.2 Spanish, Italian, Dutch and Portuguese

- [x] `Resources/Localization/{es-ES,it-IT,nl-NL,pt-PT}.json` cover every key, with Spanish
      inverted punctuation and European Portuguese spelling.
- [x] `SpanishTimeFormatter` (*son las nueve y cuarto*, *diez menos cuarto*, *es la una*,
      *mediodía*/*medianoche*, feminine *veintiuna*), `ItalianTimeFormatter` (*e un quarto*,
      *e mezza*, *meno un quarto*, *è l'una*, *ventuno*/*ventotto*/*ventitré*),
      `DutchTimeFormatter` (*kwart over*, *half tien*, *kwart voor*, *één uur*,
      *tweeëntwintig*) and `PortugueseTimeFormatter` (*e um quarto*, *e meia*,
      *menos um quarto*, *é uma hora*, *vinte e uma horas*, *meio-dia*/*meia-noite*).
- [x] 100 phrases per language under the English ids, categories, tones, weights, windows
      and active-minute gates; `WesternEuropeCatalogTests` asserts the parity and that each
      pack has its own formatter and is chosen for regional variants.
- [x] `es-MX`, `it-CH`, `nl-BE` and `pt-BR` inherit the grammar through the language match.
- [ ] **Outstanding:** native-speaker review of every string and phrase, and a voice check
      per language — none of these languages was audible in this environment.

## Completed — M2.3 Polish, Russian and Greek

- [x] `Resources/Localization/{pl-PL,ru-RU,el-GR}.json` cover every key and
      `Resources/Phrases/*.json` hold 100 phrases each under the English ids,
      categories, tones, weights, windows and active-minute gates.
- [x] `PolishTimeFormatter`: *Jest dziewiąta*, *kwadrans po dziewiątej*, *wpół do
      dziesiątej* (genitive after *do*), *za kwadrans dziesiąta*, feminine hour
      ordinals and Polish plural agreement (*minuta*, *dwie minuty*, *pięć minut*,
      *dwadzieścia jeden minut*).
- [x] `RussianTimeFormatter`: *Сейчас девять часов*, *четверть десятого*,
      *половина десятого*, *без четверти десять*, genitive hours after the half and
      *час/часа/часов*, *минута/минуты/минут* agreement including 21 and 22.
- [x] `GreekTimeFormatter`: *Είναι εννέα η ώρα*, *εννέα και τέταρτο*, *εννέα και
      μισή*, *δέκα παρά τέταρτο*, named *μεσάνυχτα*/*μεσημέρι* and a 24-hour reading
      (*δεκαεννέα και σαράντα πέντε*).
- [x] `CentralEasternEuropeTests` asserts the shared pack acceptance, automatic
      resolution (`pl`→`pl-PL`, `ru-BY`→`ru-RU`, `el-CY`→`el-GR`) and that every
      minute of the day produces a digit-free sentence in all three modes.
- [x] `local/install-all-packs.ps1` now reports and installs voices for `pl-PL`,
      `ru-RU` and `el-GR`; the test project copies phrase catalogs by wildcard so a
      new pack needs no csproj edit.
- [ ] Native-speaker review of the three packs, and a voice check per language.

## Completed — M2.4–M2.6 Arabic, Hindi, Chinese, Japanese and Korean (provisional)

- [x] `Resources/Localization/{ar,zh-Hans,zh-Hant,ja-JP}.json` cover every key and
      `Resources/Phrases/*.json` hold 100 phrases each under the English ids,
      categories, tones, weights, windows and active-minute gates.
- [x] `ArabicTimeFormatter`: *الساعة التاسعة*, *والربع*, *والنصف*, *إلا ربعًا*,
      *دقيقة واحدة*, dual *دقيقتين*, plural *خمس دقائق*, and genitive tens after
      *إلا* (*إلا عشرين دقيقة*).
- [x] `ChineseTimeFormatter` serves both scripts: *现在是九点*, *九点一刻*, *九点半*,
      *差一刻十点*, *九点零五分*; Traditional swaps the characters only, and each
      locale keeps its own catalog instead of converting.
- [x] `JapaneseTimeFormatter`: *今は九時です*, *九時半*, *九時十五分*, *九點*/**零時**,
      *正午*, plus 24-hour and exact readings (*今は九時零五分です*).
- [x] Chinese regions pick their script automatically: `zh-CN`/`zh-SG` → `zh-Hans`,
      `zh-TW`/`zh-HK`/`zh-MO` → `zh-Hant`, in both the resolver and the grammar factory.
- [x] All four locales are `IsProvisional`, so both language menus append the draft
      marker, and `MiddleEastAndEastAsiaTests` asserts the flag, pack parity, phrasing
      and per-minute coverage.
- [x] `HindiTimeFormatter`: *अभी नौ बजे हैं*, *सवा नौ*, *साढ़े नौ*, *पौने दस*,
      *नौ बजकर पाँच मिनट* and *दस बजने में दस मिनट*, with Hindi numerals spoken in words.
- [x] `KoreanTimeFormatter`: native hours (*한 시*, *두 시*, *아홉 시*), Sino-Korean
      minutes (*십오 분*, *사십오 분*), *반* for the half hour, *정오* and *자정*.
- [x] `SouthAsiaAndKoreaTests` asserts pack parity, phrasing, resolution (`hi-IN` →
      `hi`, `ko-KR` → `ko-KR`) and digit-free output for every minute of the day.
- [x] **Arabic RTL fixed where it was actually missing:** the WinForms tray menu now
      sets `RightToLeft`, the close-to-tray message box passes `RtlReading`/
      `RightAlign`, and the analog clock numerals use `LocalizationCulture` so the dial
      keeps Western digits instead of switching to ١٢٣.
- [x] **M2.7 wave 1:** `DanishTimeFormatter` (*kvart over ni*, *halv ti*, *kvart i
      ti*), `SwedishTimeFormatter` (*kvart över*, *halv tio*, *kvart i*),
      `NorwegianTimeFormatter` (*kvart over*, *halv*, *kvart på*) and
      `CzechTimeFormatter` (*čtvrt na deset*, *půl desáté*, *tři čtvrtě na deset*,
      *za pět minut deset*) with the hodina/hodiny/hodin agreement.
- [x] `WorldwideExpansionTests` covers pack parity, provisional flags, phrasing,
      regional inheritance and digit-free output for every minute of the day.
- [ ] **M2.7 waves 2–4:** Turkish, Hungarian, Romanian, Croatian, Bulgarian, Slovak,
      Slovenian, Catalan, Hebrew, Indonesian, Malay, Thai, Vietnamese and Tamil.
- [ ] Native-speaker review of every string, phrase and grammar, and a voice check per
      language.
- [ ] RTL and CJK window/tray layout remains visually unverified (no screenshot pass).

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

- 21 locales ship. The original seventeen-locale plan is fully delivered; M2.7 adds
  the Windows-voice languages it did not cover, 15 of which remain.
- Every non-English pack is a project draft: all sixteen still need a native speaker's
  review, and the seven machine-drafted ones (Welsh, Arabic, Hindi, both Chinese packs,
  Japanese and Korean) carry the visible draft marker in the UI.
- Welsh content is a draft, not a translation anyone has checked: it is marked in both
  language menus and must be replaced after native review. Its time grammar is only as
  good as the draft wording, particularly mutations and the 24-hour reading.
- Only English speech was audible in this environment; French, German, Spanish, Italian,
  Dutch, Portuguese and Welsh announcements, and their "no installed voice speaks …"
  paths, are unit-tested only. German (Microsoft Hedda) and Spanish voices ship with some
  Windows installs, but none is installed here. `local/install-all-packs.ps1` exists to
  install and expose them (`-Install`, `-Bridge`, `-Remove`); its report path runs here,
  but the elevated install and bridge paths could not be exercised in this environment.
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
2. **Native review of the sixteen drafted packs:** correct wording and grammar, then
   clear `IsProvisional` and decide which packs should keep the marker.
3. **M2.7 waves 2–4** in the order listed in the completed section, each with a
   complete pack, phrasing tests and a voice-helper row.
4. **Visual verification** of the Arabic RTL window/tray and CJK line breaking and font
   fallback, plus one audible announcement per language on a machine with the voice.
4. M3: opt-in active-session reminders, quiet hours, tone/category settings, diagnostics.
