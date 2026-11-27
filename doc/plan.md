# SoliloClock — Project Plan

**Status:** Specification / implementation handoff. M0 and M1 are implemented; see `STATUS.md` for verification, deviations and known limitations.  
**Platform:** Windows 10/11  
**Implementation:** C# / .NET / WPF  
**Name:** SoliloClock (the working name in early drafts was "Talking Clock")  
**Priority:** Small, delightful, offline-first, low-resource, genuinely useful.

## 1. Vision

Build a polished Windows **system-tray talking clock** that announces the time at user-selected intervals and occasionally makes amusing, context-aware remarks in the spirit of the classic Unix `fortune` command. It must feel like a tiny native utility, not a browser application or enterprise dashboard. A charming animated clock appears when the window is opened; the application remains useful when hidden.

**Non-goals:** Python, Electron, cloud accounts, telemetry, ads, mandatory Internet access, LLM dependencies, and a complex settings dashboard.

## 2. Technical decisions

- **C# + WPF** on a currently supported .NET release, targeting Windows 10/11. Verify installed SDK and support lifecycle before choosing the exact TFM.
- **WPF vector drawing** for animated analog clock, optional digital clock, DPI scaling, rounded window and transparency; use CompositionTarget.Rendering only while visible, otherwise lightweight timer scheduling.
- **Windows built-in speech** behind an `ISpeechEngine` interface. Start with Windows SAPI / `System.Speech` where compatible; validate voice availability and Unicode/language support. Allow a later optional Windows modern TTS adapter, but no external service is required.
- **NotifyIcon** via a small maintained WinForms interop or equivalent; use a single application instance and a hidden-to-tray window.
- **JSON settings** in `%APPDATA%\SoliloClock\settings.json`; atomic saves and safe recovery from malformed settings.
- **Bundled UTF-8 JSON phrase catalogs and localized UI resources**; user-editable overrides later.
- **No administrator privileges.** No auto-start without explicit consent. No network access required.
- Use MVVM only where it improves clarity. Avoid frameworks and dependencies unless justified. Nullable reference types, async/cancellation where needed, small testable services.

## 3. Main interface — minimal by design

A compact, attractive, resizable window with:

1. Large **animated analog clock** with smooth second hand, precise minute/hour hands and optional digital time beneath.
2. Exactly **four prominent interval buttons:** `15` · `30` · `45` · `60` (minutes). Single selection; clear active state.
3. A large **mute/unmute** toggle and a **Speak Now** button (small icons are acceptable).
4. Small theme and settings controls; no giant settings panel on the main view.
5. A rotating **caption** displaying the last spoken phrase, optionally fading in/out.
6. Close-to-tray behavior with a clear Exit command in the tray menu; explain this on first close.
7. Keyboard navigation, screen reader labels, tooltips, high-contrast fallback and reduced-motion mode.

### Interval semantics (important)

- Buttons select the *cadence*, not an arbitrary polling loop.
- Default: **15 minutes**; announcements at wall-clock quarter-hours (`:00`, `:15`, `:30`, `:45`).
- `30`: `:00` and `:30`; `60`: on the hour.
- `45`: **every 45 elapsed minutes**, anchored to the moment selected, rather than pretending it is an even subdivision of an hour. Display the next scheduled announcement in settings/tray tooltip. Preserve the anchor across app restart if desired, and document behavior.
- Only one announcement per scheduled boundary, even if timers fire repeatedly. Reschedule on clock changes, resume from sleep, timezone/DST changes, and system startup. **Do not replay missed announcements** after waking.
- Default speaking format uses natural localized time; allow 12/24-hour preference independently of UI locale.

## 4. Tray behavior

- Tray menu: **Speak Time Now**, **Mute/Unmute**, **15/30/45/60**, **Show Clock**, **Settings**, **Exit**.
- Optional left-click to open; double-click configurable if practical.
- Start minimized to tray (opt-in), start with Windows (opt-in), quiet hours, and an optional hotkey for Speak Now.
- Muting never stops the visible clock. Quiet hours silence both regular speech and jokes, except explicit Speak Now (configurable).
- Respect Windows audio output and volume; provide speech volume and rate controls.

## 5. Theme packs

Ship **five built-in themes** with consistent contrast, typography and animations. Themes must style the clock, buttons, captions, settings and tray-menu imagery where supported.

| Theme | Personality | Visuals |
|---|---|---|
| **Pink** | Playful, cute | Rose/pastel pink, lilac, soft sparkles, rounded numerals, subtle hearts |
| **Blue** | Energetic | Cobalt, sky blue, crisp highlights, sporty clock hands |
| **Black** | Minimal, moody | Near-black, graphite, steel, precise typography, restrained motion |
| **Neon** | Retro-futuristic | Electric cyan/magenta, glowing clock ticks, tasteful pulse animation |
| **Transparent** | Floating desktop clock | Adjustable window opacity, click-through optional only when explicitly enabled, always-on-top toggle |

These are aesthetic names only: **anyone can choose any theme**. Themes do not determine the voice or language. Include a reduced-motion mode and avoid distracting continuous effects while minimized. Bundle assets; no downloaded themes needed.

### Animation

- Smooth sweep second hand, gentle tick highlights, optional pendulum or pulse on the minute.
- Optional playful reaction during speech: clock face smiles/blinks or small speech bubble appears; no full-screen interruption.
- Pause animation when hidden/minimized; aim for low idle CPU and memory.

## 6. Speech, language and localization

**The first public release is English-only, and says so.** M1 ships `en-GB` UI, time grammar and phrase catalog; the README and status file state that plainly rather than implying more. Multilingual is M2, and it means a human-authored catalog per language (never machine-translated jokes presented as complete), locale-specific time grammar and, for speech, a compatible installed Windows voice. When a language does ship, use its **native script and typographic conventions**, not romanized approximations. Unicode, grapheme-aware layout, font fallback, pluralization and RTL layout are essential.

Initial language targets (UI and original phrase catalogs; speech depends on installed voices):

| Language | Native name | Example localized time style |
|---|---|---|
| English | English | It's quarter past seven. |
| French | Français | Il est sept heures et quart. |
| German | Deutsch | Es ist Viertel nach sieben. |
| Spanish | Español | Son las siete y cuarto. |
| Italian | Italiano | Sono le sette e un quarto. |
| Dutch | Nederlands | Het is kwart over zeven. |
| Portuguese | Português | São sete e quinze. |
| Polish | Polski | Jest siódma piętnaście. |
| Russian | Русский | Сейчас семь пятнадцать. |
| Greek | Ελληνικά | Είναι επτά και τέταρτο. |
| Arabic | العربية | الساعة السابعة والربع. |
| Hindi | हिन्दी | सवा सात बजे हैं। |
| Chinese (Simplified) | 简体中文 | 现在是七点十五分。 |
| Chinese (Traditional) | 繁體中文 | 現在是七點十五分。 |
| Japanese | 日本語 | 7時15分です。 |
| Korean | 한국어 | 일곱 시 십오 분입니다. |

- Use **BCP-47 locale codes** (`en-GB`, `fr-FR`, `de-DE`, `ar`, `ja-JP`, etc.). Include regional voice variants where available.
- Separate UI language, speech language and selected installed voice. Default to system locale and an appropriate installed voice.
- Voice dropdown must show only actually installed/supported voices, with **Test Voice** and a clear fallback message when a language lacks a voice. Never claim every listed language can speak without a compatible TTS voice.
- Phrase catalogs need **human-authored translations/adaptations**, especially for humor. A joke may need a culturally equivalent replacement rather than literal translation. Missing translations fall back to a simple localized time announcement or English only if user opts in.
- Language-specific time grammar and number handling belong in locale-specific formatter implementations, not naive concatenation.

**Rollout:** M2 is delivered in language stages (see §14). The localization
infrastructure and the English reference landed first; each later stage adds one
region at a time, beginning in Western Europe. Nothing is marked shipped until
its UI strings, time grammar, catalog and voice guidance are real.
- Use Windows-compatible fonts with fallback for CJK, Arabic shaping, Devanagari and other scripts; test bidirectional mixed numbers in Arabic.
- Phrase text must be safely treated as data, not executable code.

## 7. The `fortune`-style personality engine

Use a **local catalog of short original lines** with categories, weights, context predicates and cooldowns. `fortune` is inspiration for the behavior, **not** a requirement to bundle third-party quote databases or their licenses.

### Categories

- `time`: playful ways to announce the actual time.
- `computer`: jokes about coding, compiling, debugging, terminal tabs and build failures.
- `break`: friendly suggestions to stretch, rest eyes or go outside.
- `late-night`: gentle midnight absurdity, only when not in quiet hours.
- `morning`: coffee, optimism, regret.
- `afternoon`: meetings, lunch, diminishing ambition.
- `evening`: winding down, unfinished projects.
- `fortune`: strange aphorisms and nonsense wisdom.
- `rare`: occasional easter eggs, very low probability.
- `seasonal`: optional non-religious seasonal lines; locale-aware and opt-in.
- `user`: custom phrases from user files (later milestone).

### Selection rules

- **Always announce the actual time clearly first**; jokes are an optional second sentence or standalone rare interlude, never a replacement for the time.
- Defaults: 25% of scheduled announcements include a joke; configurable 0/10/25/50/100%.
- No repeat within the last 20 selected phrases (or until the catalog is exhausted); weighted random with a recent-history ring buffer.
- Avoid inappropriate late-night chatter, insults, repeated nagging or invasive claims.
- Avoid false claims like "you've been typing for three hours" unless a measured metric supports it.
- Content tone presets: **Polite**, **Cheeky**, **Unhinged**. Default Cheeky. All language packs should work with these tags.
- Provide a **Family-friendly** content filter and an option to disable particular categories.
- Optional **fortune-only Speak Now** button/menu action.

## 8. Computer-usage reminders

- **"You've been using your computer for three hours. Get outside."** This must use an actual, documented measure: app-observed active-session duration since app launch or unlock, not invented usage.
- Better: distinguish `elapsed session time` from `estimated active time`. Windows idle detection can estimate active time; do not call it a complete history of computer usage.
- Default reminder thresholds: **60, 120, 180 minutes** of estimated active time; user-configurable and disabled by default until consented to.
- After each reminder, cooldown at least 60 minutes. Reset/adjust when system locks, sleeps or user is idle for a configurable period.
- Optional screen-break nudges: 20-20-20 eye-rest reminder; stand up/stretch; drink water; lunch reminder.
- Avoid monitoring app names, keystrokes, window titles or browsing history. No telemetry. User can turn reminders off instantly.
- Optional "meeting mode" (suppress jokes), "focus mode" (time only), and "do not disturb" (mute all).

## 9. Original English phrase bank — seed content

**All lines below are newly written for this project.** Adapt naturally for each language rather than translating mechanically. Time announcements are generated separately and prepended when appropriate.

### Coding and computers

1. Another quarter hour has compiled successfully. Unlike your project.
2. Your compiler sends its regards. Mostly warnings.
3. Time flies. Your build system prefers walking.
4. Somewhere, a semicolon is ruining someone's afternoon.
5. You have seventeen tabs open and absolutely no answers.
6. The bug has been promoted to a feature. Congratulations.
7. Your CPU believes in you. Your RAM has concerns.
8. It's a lovely day to delete a cache and pretend it helped.
9. The code works. Please do not touch anything.
10. Have you tried turning your expectations off and on again?
11. One does not simply fix the final warning.
12. Your TODO list has requested its own TODO list.
13. The documentation is accurate in a parallel universe.
14. Another hour, another mysterious dependency.
15. A clean build is a beautiful and suspicious thing.
16. Your terminal has seen things it cannot unsee.
17. Today's forecast: scattered exceptions with a chance of rollback.
18. You can close one browser tab. As a treat.
19. The debugger is not judging you. Probably.
20. Your future self has filed a complaint about this shortcut.
21. Git remembers. Git always remembers.
22. Somewhere in this repository is a function named `final_final`.
23. The tests are green. Suspiciously green.
24. Your laptop fan is composing experimental jazz.
25. Have you considered writing fewer configuration files? Neither have we.

### Breaks and outside

26. You've been at this for three hours. The outdoors would like a word.
27. Your chair has enjoyed enough of your company.
28. The sky is still available without a subscription.
29. Go and inspect a tree. No debugger required.
30. Your eyes have requested a maintenance window.
31. A short walk may resolve dependencies in your brain.
32. The grass has not been updated, but it's still worth touching.
33. Water: the original productivity plugin.
34. Stretch. Even your stack has limits.
35. You are permitted to leave the computer unattended. It will cope.
36. Your coffee is not a substitute for daylight.
37. This is your friendly reminder that knees have a purpose.
38. Step away from the screen. The pixels will remain loyal.
39. Fresh air has excellent backwards compatibility.
40. Your keyboard suggests a brief separation.

### Morning and afternoon

41. Good morning. The bugs have been awake for hours.
42. Rise and shine. Or rise and negotiate.
43. Breakfast is not an optional dependency.
44. The day is young. Your inbox is ancient.
45. Coffee has entered the chat.
46. It's almost lunchtime. Even the clock looks hungry.
47. The afternoon has arrived with no useful release notes.
48. Another productive morning of moving windows around.
49. Your calendar says meeting. Your soul says absolutely not.
50. The sun is doing its best. Consider joining it.

### Evening and late night

51. The evening shift is now being supervised by a clock.
52. Another day of heroic progress and questionable decisions.
53. It's getting late. Your future self would appreciate sleep.
54. The moon has checked in. Have you checked out?
55. Midnight: when every idea sounds like a startup.
56. Your pillow has opened a support ticket.
57. This is the hour when variable names become poetry.
58. The night is quiet. Except for your cooling fan.
59. Your code can wait until morning. The bugs aren't going anywhere.
60. Tomorrow is another chance to rename everything.

### Fortune-style absurdities

61. A watched progress bar never admits defeat.
62. Beware the wisdom of clocks: they are always counting on something.
63. A wise programmer knows when to stop. A legendary one has a timer.
64. Time is money, but this clock accepts neither.
65. The universe is expanding. So is your downloads folder.
66. Do not argue with a clock. It has all the time in the world.
67. If life gives you lemons, check the package checksum.
68. Your destiny is being processed. Please do not refresh.
69. An empty inbox is a myth told to junior developers.
70. The secret to happiness is somewhere under Settings → Advanced.
71. Every great invention began with someone avoiding another task.
72. There are two kinds of people: those who back up, and future cautionary tales.
73. A wise person asks questions. A foolish person clicks Update All.
74. The clock knows what you did last summer. It was probably debugging.
75. Somewhere, an ancient server is still running perfectly. Nobody knows why.

### Rare and ridiculous

76. Attention. Your mouse has been elected project manager.
77. This announcement was approved by the International Council of Minutes.
78. The clock has decided to pursue a career in interpretive dance.
79. Please remain calm. An unusually punctual second is approaching.
80. The next minute is brought to you by the previous minute.
81. I have counted every second. Some of them were deeply disappointing.
82. Your desk has requested a performance review.
83. The hour hand and minute hand are no longer speaking.
84. I was going to tell you a time-travel joke, but you didn't like it.
85. A small committee of clocks has voted to postpone Tuesday.
86. The seconds are unionising. Expect delays.
87. You have unlocked the legendary achievement: Still Sitting There.
88. I regret to inform you that tomorrow has not been cancelled.
89. The concept of punctuality is currently in beta.
90. The clock would like to thank you for your continued existence.

### Optional cheeky personality

91. You've been staring at that screen long enough to become a wallpaper.
92. Another fifteen minutes of pretending this is the final fix.
93. Your productivity graph has been replaced by a shrug.
94. This is a clock, not a life coach. Yet here we are.
95. You have permission to stop optimizing things nobody will notice.
96. Perhaps the bug is not in the code. Perhaps the bug is destiny.
97. Your brilliant plan appears to involve more coffee.
98. Have you tried explaining the problem to a rubber duck with management experience?
99. One more refactor and the project may become self-aware.
100. It is now time to consider whether you actually needed another feature.

## 10. Phrase file format

Store UTF-8 JSON files in `Resources/Phrases/<locale>.json`. Example:

```json
{
  "locale": "en-GB",
  "phrases": [
    {
      "id": "computer.001",
      "text": "Another quarter hour has compiled successfully. Unlike your project.",
      "category": "computer",
      "tone": "cheeky",
      "weight": 1,
      "minActiveMinutes": null,
      "timeWindow": null
    },
    {
      "id": "break.003",
      "text": "You've been at this for three hours. The outdoors would like a word.",
      "category": "break",
      "tone": "cheeky",
      "weight": 1,
      "minActiveMinutes": 180,
      "timeWindow": null
    }
  ]
}
```

Use stable IDs across translations. Catalogs may be partial; validate schema and skip invalid entries with logged diagnostics. Include phrase tests to catch duplicate IDs, empty text and unsupported locale tags. Don't interpolate arbitrary untrusted strings into executable commands.

## 11. Settings

- Announcement interval (15/30/45/60), next-announcement preview.
- UI language, spoken language, installed voice, speech rate and volume.
- 12/24-hour format; optional natural time expressions vs exact numeric time.
- Theme, opacity, always-on-top, animations, reduced motion.
- Joke probability, tone, enabled categories, family-friendly mode.
- Quiet hours, mute, start with Windows, start minimized.
- Reminder opt-in, thresholds, active-time definition and cooldown.
- Speak Now hotkey; optional chime before announcement; optional hour-only chime.
- Restore defaults; export/import settings; accessible help/about page.

## 12. Suggested repository layout

```text
SoliloClock/
  doc/plan.md
  README.md
  STATUS.md
  LICENSE
  .editorconfig
  .gitignore
  SoliloClock.sln
  src/
    SoliloClock.App/
      App.xaml
      MainWindow.xaml
      Views/
      ViewModels/
      Services/
      Models/
      Themes/
      Resources/
        Localization/
        Phrases/
        Icons/
  test/
    SoliloClock.Tests/
  tools/
    build.ps1
    publish.ps1
```

Add a sensible `.editorconfig` for C# and XAML; consistent naming, formatting and nullable warnings. Choose a permissive OSS license after deciding whether third-party assets are bundled. Keep theme artwork and phrase catalogs clearly licensed.

## 13. Robustness and test cases

- First run without a suitable voice; silent fallback and actionable explanation.
- Speech queue must not stack announcements if the computer is busy or wakes after hours asleep. Cancel or skip stale messages.
- No duplicate announcements after DST jumps, clock changes or repeated timer callbacks.
- Long localized speech must not block UI; no overlapping voice playback.
- Handle missing/broken settings, unsupported locale, malformed phrase JSON and audio-device changes.
- Test on Windows 10 and 11; 100%, 150%, 200% DPI; dark/high-contrast; multi-monitor; transparent window; screen readers.
- Test native Arabic RTL, Hindi shaping, CJK fallback and mixed digits.
- Test 15/30/45/60 scheduling, midnight rollover, sleep/resume and time-zone changes with an injectable clock.
- Test long-run idle CPU/memory and tray-only resource use; no busy polling.
- Include a visible **Diagnostics** page: next announcement, last speech time, selected voice, locale, recent errors; optional local log file with rotation.
- No network, telemetry, keyboard logging or invasive monitoring.

## 14. Delivery milestones and acceptance criteria

### M0 — Skeleton — complete
- [x] WPF project builds and launches with no console window.
- [x] Tray icon, Show/Hide/Exit and single-instance behavior work.
- [x] Basic settings persistence, `.editorconfig`, README and build scripts.

### M1 — Minimum lovable product — complete
- [x] Attractive animated analog clock and digital readout.
- [x] Exactly four interval buttons: 15, 30, 45, 60.
- [x] Local Windows speech announces accurate time; Speak Now and Mute work.
- [x] Correct wall-clock scheduling, with explicit 45-minute semantics.
- [x] Five selectable themes, including functional transparency.
- [x] App runs in tray and doesn't waste CPU while hidden.
- [x] English phrases, 25% joke rate, no immediate repeats.
- [x] No Python, Electron, login, cloud service or admin requirement.

### M2 — Multilingual, delivered in language stages

A language ships only when its UI strings, time grammar and phrase catalog are
all present, and is complete only once a human has authored them. A language
without a compatible installed Windows voice still ships its text; the Voice menu
says plainly that it cannot speak.

**Provisional locales.** A locale whose wording is machine-drafted may ship only
with `LocaleDefinition.IsProvisional` set, which makes both language menus append
a draft marker. Provisional text is a placeholder awaiting a native speaker, is
never described as reviewed, and a provisional locale is never recorded as a
completed language.

#### M2.1 — Localization foundation — complete
- [x] Per-locale UI catalogs at `Resources/Localization/<locale>.json`, layered
      over the English base so a partial translation keeps reviewed wording.
- [x] `UiLanguage` and `SpeechLanguage` are actually consumed; UI language,
      speech language and voice are independent, and invalid tags fall back to automatic.
- [x] **Language is automatic by default.** The Windows display language selects the
      best shipped catalog (`fr-CA` → `fr-FR`; a language with no catalog → English,
      stated in the status line). Tray **Options → Language** and **Announce in**
      override it and apply immediately, for testing or for announcing in another
      language.
- [x] `TimeFormatterFactory` selects grammar by locale instead of hardcoding
      English; `en-GB` was the only grammar during M2.1, and M2.2 adds French.
- [x] Installed voices are listed with the culture they speak, with **Test voice**
      and an explicit "no installed voice speaks <language>" state.
- [x] With no voice pinned, announcements use an installed voice that speaks the
      announcement language (exact region first, else any voice of that language,
      else the Windows default), chosen again on each announcement so a voice
      installed later is used without a restart.
- [x] RTL layout, WPF language and clock numeral culture follow the locale; the
      clock keeps Gregorian wall time and Western digits under any locale.
- [x] Grapheme-safe tray truncation (no split surrogate pairs, emoji or combining marks).
- [x] Product naming is SoliloClock everywhere (window, tray, tooltip, messages).
- [x] Tests for catalog completeness, fallback, RTL direction, tag repair and formatter selection.

#### M2.2 — Western Europe
`fr-FR` · `de-DE` · `es-ES` · `it-IT` · `nl-NL` · `pt-PT`

**French (`fr-FR`) — complete**
- [x] Human-authored UI catalog (`Resources/Localization/fr-FR.json`) covering every
      key (asserted by test), with French punctuation (non-breaking space before `: ; ? !`, guillemets
      with inner spacing) and typographic apostrophes.
- [x] `FrenchTimeFormatter` with tests for the hour, quarter, half, *moins le quart*,
      minute-to forms, midnight, noon, day-part suffixes and 24-hour reading.
- [x] 100 human-authored phrases under the same stable ids and metadata as `en-GB`.
- [x] `fr-FR` promoted in `LocaleRegistry`; `fr-CA` and other French locales inherit
      the French grammar through the language match.
- [ ] Voice check on a machine with an installed French voice (or confirm the
      "no installed voice speaks Français" state) — not verified in this environment.

**Welsh (`cy-GB`) — provisional, native review outstanding**
- [x] UI catalog (`Resources/Localization/cy-GB.json`) covering every key, 100 phrases
      under the same stable ids and metadata as `en-GB`, and `WelshTimeFormatter`
      (*o'r gloch*, *chwarter wedi*, *hanner awr wedi*, *chwarter i*, soft mutation
      after *yn* and *i*, midnight/noon named).
- [x] `cy-GB` ships flagged `IsProvisional`; both language menus append the draft
      marker, and a Welsh Windows resolves to it automatically.
- [ ] Native-speaker review of every string, phrase and the time grammar; replace the
      draft wording and clear the flag.
- [ ] Voice check with an installed Welsh voice — Techiaith TTS is a third-party
      SAPI 5 install, never bundled or committed to this repository. Not verified in
      this environment.

**German (`de-DE`) — complete**
- [x] Human-authored UI catalog (`Resources/Localization/de-DE.json`) covering every
      key, in the formal *Sie* form, with German ß and „…“ quotation marks.
- [x] `GermanTimeFormatter`: *Es ist neun Uhr*, *Viertel nach neun*, *halb zehn*
      (halfway to ten, not half past nine), *Viertel vor zehn*, *ein Uhr* (never
      "eins Uhr"), named *Mittag*/*Mitternacht*, and a literal 24-hour reading
      (*neunzehn Uhr fünfundvierzig*). Minutes outside the quarters are read as clock
      time (*neun Uhr zwanzig*) rather than a region-specific *zwanzig nach neun*.
- [x] 100 phrases under the same stable ids and metadata as `en-GB`.
- [x] `de-DE` promoted in `LocaleRegistry`; `de-AT` and other German locales inherit
      the grammar through the language match.
- [ ] Voice check on a machine with an installed German voice — not verified here.

**Spanish (`es-ES`), Italian (`it-IT`), Dutch (`nl-NL`) and Portuguese (`pt-PT`) — complete**
- [x] UI catalogs covering every key, 100 phrases each with the English ids and metadata,
      and a dedicated formatter per language: *son las nueve y cuarto / menos cuarto*,
      *sono le nove e mezza*, *het is half tien*, *são nove horas e um quarto*.
- [x] Language-specific grammar: singular *es la una* / *è l'una* / *é uma hora*, feminine
      hour agreement (*veintiuna*, *ventiuna*, *vinte e uma horas*), Dutch *één* versus
      *een*, and Italian elisions (*ventuno*, *ventotto*, *ventitré*).
- [x] Spanish inverted `¿`/`¡` and European Portuguese spelling (*dezanove*, *catorze*)
      used consistently in the catalogs.
- [x] `de-AT`, `es-MX`, `it-CH`, `nl-BE` and `pt-BR` inherit their grammar by language match.
- [x] Shared acceptance test: complete UI catalog, 100 metadata-identical phrases, its own
      formatter and automatic resolution for each of the six non-English packs.
- [ ] Native-speaker review of every string and phrase (only Welsh is flagged in the UI).
- [ ] Voice check per language on a machine with the matching SAPI voices.
- [ ] State which installed SAPI voices can speak each language; Test voice works.

#### M2.3 — Central, Northern and Eastern Europe — complete
`pl-PL` · `ru-RU` · `el-GR`
- [x] Same acceptance items as M2.2: complete UI catalog, 100 metadata-identical
      phrases and a dedicated formatter for each language.
- [x] `PolishTimeFormatter`: *Jest dziewiąta*, *kwadrans po dziewiątej*, *wpół do
      dziesiątej* (genitive after *do*), *za kwadrans dziesiąta*, feminine hour
      ordinals, and plural agreement for minutes (*minuta*, *dwie minuty*, *pięć
      minut*, *dwadzieścia jeden minut*).
- [x] `RussianTimeFormatter`: *Сейчас девять часов*, *четверть десятого*,
      *половина десятого*, *без четверти десять*, genitive hours after the half, and
      *час/часа/часов* plus *минута/минуты/минут* agreement including 21 and 22.
- [x] `GreekTimeFormatter`: *Είναι εννέα η ώρα*, *εννέα και τέταρτο*, *εννέα και
      μισή*, *δέκα παρά τέταρτο*, named *μεσάνυχτα*/*μεσημέρι*, and a 24-hour reading
      in words (*δεκαεννέα και σαράντα πέντε*).
- [x] Plural and case agreement exercised in formatter tests, and every minute of the
      day asserted to produce a digit-free sentence in natural, exact and 24-hour modes.
- [ ] Native-speaker review and a voice check per language.

#### M2.4 — Middle East and right-to-left — Arabic shipped, provisional
`ar`
- [x] Complete UI catalog, 100 phrases and `ArabicTimeFormatter`: feminine hour
      ordinals, *والربع*, *والنصف*, *إلا ربعًا*, dual *دقيقتين*, plural
      *خمس دقائق*, and the genitive tens that *إلا* governs.
- [x] `ar-SA`, `ar-EG` and other Arabic regions inherit the grammar by language match.
- [x] The window flows right-to-left through `UiText.FlowDirection`; the clock keeps
      Gregorian wall time and Western digits, and the time is spoken in words.
- [ ] Full RTL window and tray layout verification, and mixed-direction number checks.
- [ ] Native-speaker review; the pack is provisional until then.

#### M2.5 — South Asia — Hindi shipped, provisional
`hi`
- [x] Complete UI catalog, 100 phrases and `HindiTimeFormatter`: *अभी नौ बजे हैं*,
      *सवा नौ*, *साढ़े नौ*, *पौने दस*, *नौ बजकर पाँच मिनट*, *दस बजने में दस मिनट*,
      with the numbers spoken as Hindi words.
- [x] `hi-IN` and other Hindi regions inherit the grammar by language match.
- [ ] Devanagari shaping and font-fallback checks.
- [ ] Native-speaker review; the pack is provisional until then.

#### M2.6 — East Asia — complete, provisional
`zh-Hans` · `zh-Hant` · `ja-JP` · `ko-KR`
- [x] Simplified and Traditional Chinese each ship an independent UI catalog and 100
      phrases, sharing `ChineseTimeFormatter` for numerals and the *一刻*/*半*/*差一刻*
      forms; the region picks the script (`zh-CN` → Simplified, `zh-TW`/`zh-HK` →
      Traditional).
- [x] `JapaneseTimeFormatter`: *今は九時です*, *九時半*, *九時十五分*, *正午* and *零時*.
- [x] Every minute of the day asserted to produce a digit-free sentence in all three
      modes for both Chinese scripts and Japanese.
- [x] `KoreanTimeFormatter`: native hours (*한 시*, *두 시*, *아홉 시*), Sino-Korean
      minutes (*십오 분*), *반* for the half hour, *정오* and *자정*.
- [ ] CJK line breaking, font fallback and a native-speaker review; the packs are
      provisional until then.

#### M2.7 — Worldwide: every language Windows can speak (in progress)

The original plan listed 17 locales, but Windows ships text-to-speech voices for many
more. M2.7 closes that gap so the clock can be spoken in every language the operating
system itself offers, plus Welsh (vendor voice only). Languages Windows does *not*
speak — Ukrainian, for example — are deliberately out of scope until a SAPI 5 voice
provider exists.

| Language | Tag | Status |
|---|---|---|
| Danish | `da-DK` | shipped, provisional |
| Swedish | `sv-SE` | shipped, provisional |
| Norwegian Bokmål | `nb-NO` | shipped, provisional |
| Czech | `cs-CZ` | shipped, provisional |
| Turkish | `tr-TR` | planned |
| Hungarian | `hu-HU` | planned |
| Romanian | `ro-RO` | planned |
| Croatian | `hr-HR` | planned |
| Bulgarian | `bg-BG` | planned |
| Slovak | `sk-SK` | planned |
| Finnish | `fi-FI` | planned |
| Slovenian | `sl-SI` | planned |
| Catalan | `ca-ES` | planned |
| Hebrew | `he-IL` | planned |
| Indonesian | `id-ID` | planned |
| Malay | `ms-MY` | planned |
| Thai | `th-TH` | planned |
| Vietnamese | `vi-VN` | planned |
| Tamil | `ta-IN` | planned |

That is 21 shipped locales (20 Windows languages plus Welsh) of 36 targeted, with 15
Windows languages left.

- [x] Wave 1 (Nordic + Czech): formatter, 71-key catalog, 100 phrases and tests each.
      Danish and Swedish count the half hour towards the coming hour (*halv ti*,
      *halv tio*); Norwegian uses *på* for minutes to the hour; Czech needs
      nominative/accusative/locative forms and *hodina/hodiny/hodin*.
- [ ] Wave 2: Turkish, Hungarian, Romanian, Croatian, Bulgarian, Slovak, Finnish.
- [ ] Wave 3: Slovenian, Catalan, Hebrew (right-to-left), Indonesian, Malay.
- [ ] Wave 4: Thai, Vietnamese, Tamil, including script shaping and font fallback.
- [ ] Native-speaker review of each pack; all stay provisional until then.

**Stage gate:** a language is promoted out of "planned" in `LocaleRegistry` only
when its catalog, formatter and tests exist; it is recorded as complete only after
human authorship. `STATUS.md` records what shipped, what is still a draft, and
which languages lack a compatible voice.

### M3 — Delight and reminders
- [ ] Opt-in usage/break reminders, measured session activity and cooldowns.
- [ ] Quiet hours, tone presets, categories, rare easter eggs and optional chimes.
- [ ] Speech bubble animation, reduced motion, optional hotkey.

### M4 — Polish and distribution
- [ ] Portable self-contained Windows build (consider x64 and ARM64 if practical).
- [ ] Installer optional; reproducible release packaging and checksums.
- [ ] Automated tests, clean logging, documentation and accessibility pass.

## 15. Instructions for the implementing agent

1. **Read this entire plan before coding.** First report any technical conflicts or ambiguities, then choose simple, defensible defaults.
2. Build **M0 and M1 first**, end-to-end. Do not stall the project by trying to translate sixteen languages immediately.
3. Prefer working code, running tests and a runnable Windows binary over screenshots or speculative architecture.
4. Keep the GUI attractive and playful, but the underlying scheduler, speech engine and settings testable independently.
5. Verify APIs/packages against the installed SDK; do not invent packages or unsupported TTS APIs.
6. Ensure the **four buttons really work**, all five themes are visually distinct, and Speak Now works without waiting for a timer.
7. Explain any language whose speech cannot work without a compatible installed Windows voice.
8. Preserve user files; do not write into unrelated directories or require elevation.
9. Provide clear `dotnet build`, `dotnet test`, and `dotnet publish` instructions, plus a release artifact.
10. Maintain `STATUS.md` with completed work, test results, known limitations and the next actionable steps.

## 16. Optional future ideas (not blockers)

- "Clock personalities": prim librarian, sleepy robot, melodramatic announcer, dry British sarcasm, pirate, old radio host; keep text and voice independently selectable.
- Clock face mascots that react to announcements; tiny pixel pets; special animation on the hour.
- Custom user phrase packs, drag-and-drop import/export, community theme packs.
- Optional Westminster-style chime, synthesized tick-tock, custom user sound packs.
- An optional minimalist always-on-top desktop widget and a hotkey to reveal it.
- Optional scheduled announcements for lunch, meetings or bedtime (local only).
- Optional calendar integration only if explicitly requested and with privacy controls.
- A "compliment me" mode, "roast me" mode, and a "shut up, I'm concentrating" mode.
- Optional `fortune` import with licensing review; never assume fortune collections are freely redistributable.
- Portable mode: store settings beside the executable only when explicitly enabled and writable.

---

**Definition of success:** A user installs or runs a small Windows app, chooses **15**, selects **Neon** (or **Pink**), closes it to the tray, and hears a natural, correctly timed announcement with an occasional genuinely funny line. It works offline, looks good, stays quiet when asked, and doesn't become a second job to configure.
