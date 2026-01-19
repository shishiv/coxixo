# Project Milestones: Coxixo

## v1.0 MVP (Shipped: 2026-01-19)

**Delivered:** Voice-to-clipboard transcription with push-to-talk hotkey, Azure Whisper integration, and brand-compliant UI.

**Phases completed:** 1-4 (9 plans total)

**Key accomplishments:**

- System tray app with single-instance mutex and proper icon cleanup
- Global push-to-talk hotkey with low-level keyboard hook (WH_KEYBOARD_LL)
- NAudio microphone capture with 16kHz mono WAV encoding for Whisper API
- Azure OpenAI Whisper API integration with exponential backoff retry
- Walkie-talkie audio feedback (ascending/descending chirps)
- Brand-compliant tray icons with 500ms recording animation
- Dark-themed settings UI with hotkey customization and API connection test

**Stats:**

- 1,696 lines of C#
- 46 commits
- 4 phases, 9 plans, ~25 tasks
- 2 days from project init to ship (2026-01-17 → 2026-01-19)

**Git range:** `e8c03eb` → `6dd91e2`

**Known limitations (v1.1 backlog):**

- Hotkey only supports single keys (F8, Home, PageUp) — modifier combinations (Ctrl+X, Shift+Y) not yet recognized

**What's next:** v1.1 — Hotkey improvements and additional configuration options

---
