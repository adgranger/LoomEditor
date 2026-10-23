# P7-25 — The manual test plan: Narrator, NVDA, Accessibility Insights

- **Status:** todo
- **Depends on:** P7-24
- **Size:** M (docs only)

## Goal

`docs/testing/phase-7-manual-tests.md` is the complete, runnable manual test plan of the roadmap ("Manual test plan with Narrator and NVDA"): setup, scripts with the expected speech, and a result column, ready for the maintainer.
The sections written by earlier cards (1 screen reader mode, 2 tab focus, 3 and 4 text pattern, 5 events, 6 high contrast, 7 self-check) are kept and cleaned; the new sections 8–11 are added.

## Sources

None to port. Narrator: `Win + Ctrl + Enter` toggles it; `Caps Lock + Right/Left` (or `Insert`) is the Narrator key; Settings → Accessibility → Narrator. NVDA: <https://www.nvaccess.org/> (free), `Insert + T`, speech viewer `NVDA menu → Tools → Speech viewer`.
Accessibility Insights for Windows: <https://accessibilityinsights.io/> (FastPass, Live inspect, Events).

## Files to create or change

- `docs/testing/phase-7-manual-tests.md` — change: the header, the setup section, sections 8–11, the known-limits section; renumber nothing in sections 1–7
- `docs/testing/README.md` — change **only if** it exists and lists the manual test files (`ls docs/testing`): add the phase 7 file
- `src/LoomEditor.WinUI/Accessibility/README.md` — change: link to the plan

## Design decisions (already made — do not change)

- Header: Windows build, Narrator version, NVDA version, Accessibility Insights version, display scale, date and tester (blank fields, like `phase-6-manual-tests.md`), then **"Setup"**: Gallery on the **Native AOT** build (README §6), a C# sample loaded, Windows 11,
  how to start Narrator and NVDA, how to open the NVDA speech viewer, and the rule "run section 8 with Narrator only, section 9 with NVDA only; speech is compared with the *Expected speech* column, wording may differ slightly: judge by content and order".
- **Section 8 — Narrator** (table `# | Steps | Expected speech | Result`), at least these rows (write the steps precisely, the editor holds `class Foo {\n    void Bar() {}\n}` unless stated):
  8.1 focus the editor (Tab into it) → "Code editor, edit" (control type and name); 8.2 `Right` → the next character; 8.3 `Down` → the next line (full line text); 8.4 `Ctrl+Right` → the next word; 8.5 `Home` / `End` → the character at the new caret;
  8.6 `Shift+Right` ×3 → "selected" + the text; 8.7 type `x` → the typed character; 8.8 `Backspace` → the deleted character; 8.9 empty line → "blank"; 8.10 Narrator "read current line" / "read current word" (`Caps+L`, `Caps+K`) read the line / word at the caret;
  8.11 word wrap on, long line: `Down` reads the **visual** line; 8.12 `Ctrl+F` → the find widget is announced and its controls have names ("Find", "Match Case", "Previous Match", …); 8.13 find results announced (P7-13 messages);
  8.14 `F7` word highlight announcement (P7-14); 8.15 `Ctrl+M` announcements (P7-16); 8.16 multi-cursor `Ctrl+Alt+Down`: Narrator does not crash and reads the primary caret; 8.17 undo `Ctrl+Z` reads the restored text;
  8.18 Narrator scan mode (Caps+Space) does not trap the keyboard: the editor still receives typing after leaving scan mode; 8.19 Narrator's "caret browsing" is not required; 8.20 folded region: `Down` over a folded `{ … }` reads the line, the hidden text is not read until expanded
  (check and record what happens; Expected: the folded lines are skipped by `Down`, but exist in the document text)
- **Section 9 — NVDA** (same table shape): 9.1 focus → "edit, multi line, Code editor"; 9.2 `Right` / `Left` read characters; 9.3 `Down` / `Up` read lines; 9.4 `Ctrl+Right` / `Ctrl+Left` read words; 9.5 `NVDA+Up` (say line) and `NVDA+Down` (say all) read from the caret to the end;
  9.6 selecting with `Shift+Arrow` announces selection changes; 9.7 typing echoes characters / words per NVDA settings; 9.8 review cursor (numpad 7/8/9 or laptop layout) reads lines, words and characters of the document without moving the caret (check the review cursor can reach the text);
  9.9 find results (P7-13); 9.10 `F7` (P7-14); 9.11 `Ctrl+M` (P7-16); 9.12 word wrap + long line reads the visual line; 9.13 NVDA "browse mode" is not entered on the editor (it is an edit control); 9.14 the NVDA speech viewer shows no repeated or empty announcements while holding `Right` for 3 seconds (the coalescer, P7-15);
  9.15 `NVDA+F1` (developer info) shows the Text pattern as supported (or the UIA class `LoomEditor`)
- **Section 10 — Accessibility Insights for Windows**: 10.1 FastPass on the Gallery window: **no failures on the editor** (the known expected issues are written in the section "Known limits"); 10.2 Live inspect: the Text, Value, TextPattern2 patterns; `IsKeyboardFocusable` true; 10.3 Events recorder: selection and text events when typing (P7-19);
  10.4 the tab stop order Gallery controls → editor → status; 10.5 contrast check (the "color contrast" tool) of the editor in Dark Modern, Light Modern and the two bundled high contrast themes: record the ratios of the foreground on the background (compare with the P7-20 test floors);
  10.6 keyboard-only: every Gallery control and the find widget can be reached and operated without a mouse (the editor can be left with `Ctrl+M` then `Tab`)
- **Section 11 — Regression**: 11.1 the phase 3 – 6 manual lists still pass for typing, IME composition (Microsoft Japanese IME), the find widget and the minimap with a screen reader **off** (nothing slowed down: hold `Right` and scroll on "Large C# (20 000 lines)"), 11.2 with Narrator **on**, "Large C# (20 000 lines)" still scrolls and types without freezing
  (record the observed frame rate / lag in the Result column).
- **"Known limits"** section (write it from ADR 0010 "Not in phase 7" and what the earlier cards decided): colors and font styles are reported as uniform; no annotations; the screen reader mode is detected when the editor gets the focus; `Intl.Segmenter` words (CJK) are runs;
  hidden folded text belongs to the line before it; injected text is not in the UIA text; the standard Ctrl+Arrow word moves are used in screen reader mode (Monaco's `cursorWordAccessibility*` commands are not bound); no accessibility help dialog; no reduced motion.
  Each limit: one sentence, with the ADR / card where it was decided.
- Every row has an empty **Result** cell, and the file ends with a **"Findings"** table (`# | Row | What happened | Severity | Follow-up card`) for the maintainer.
- Only docs change; **no code, no test**. If a row cannot be written because a feature does not behave as the card said, write the row as the card says and add it to the "Known limits" with "to verify".

## Tests

None (docs). The review is: every row of the roadmap's checklist is covered (text pattern → 3, 4, 7; events → 5, 8, 9; live regions / announcements → 5.3 – 5.5, 8.13 – 8.15, 9.9 – 9.11; high contrast → 6, 10.5; Narrator and NVDA → 8, 9).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings (nothing changed but docs)
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The file has sections 1–11, "Known limits" and "Findings"; no row without steps or expected result
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Running the plan (the maintainer does), JAWS, Windows Magnifier, voice access, any code change.

## Suggested commit message

`docs: add the phase 7 manual test plan for Narrator, NVDA and Accessibility Insights`
