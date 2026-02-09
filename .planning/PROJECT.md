# Coxixo

## What This Is

A lean Windows system tray app for voice-to-clipboard transcription. Push-to-talk with a global hotkey captures speech, sends it to Azure OpenAI Whisper, and puts the transcribed text directly in the clipboard — ready to paste anywhere.

**Slogan:** "Fale. Solte. Cole."

## Core Value

Frictionless voice input: hold a key, speak, release, paste. Nothing else.

## Current Milestone: v1.1 Configuration & Flexibility

**Goal:** Expand hotkey support, add device and language selection, and enable auto-start — making Coxixo configurable for different workflows.

**Target features:**
- Hotkey modifier support (Ctrl+X, Shift+Y combinations)
- Microphone selection (choose from available devices)
- Transcription language selection (PT, EN, auto-detect)
- Windows startup option

## Current State (v1.0 shipped)

- **Codebase:** 1,696 lines of C#, .NET 8 WinForms
- **Tech stack:** NAudio 2.2.1, Azure.AI.OpenAI 2.1.0, System.Drawing
- **Architecture:** ApplicationContext pattern, static services, DPAPI encryption
- **Status:** Fully functional with brand-compliant UI

## Requirements

### Validated

- System tray presence with minimal footprint — v1.0
- Global push-to-talk hotkey (hold to record, release to transcribe) — v1.0
- Audio capture from default microphone — v1.0
- Azure OpenAI Whisper API integration for transcription — v1.0
- Transcription result copied to clipboard automatically — v1.0
- Audio feedback (beep on start/stop recording) — v1.0
- Visual feedback (tray icon changes while recording) — v1.0
- Settings UI (API credentials, hotkey configuration) — v1.0
- Portuguese BR as initial language — v1.0
- Fast startup and low memory usage — v1.0
- Brand-compliant tray icons (3-bar sound wave design) — v1.0
- Dark-themed settings window — v1.0

### Active (v1.1)

- [ ] Hotkey modifier support (Ctrl+X, Shift+Y combinations)
- [ ] Microphone selection (choose from available devices)
- [ ] Transcription language selection (PT, EN, auto-detect)
- [ ] Windows startup option

### Future

- [ ] Recent transcription in tray menu

### Out of Scope

- Local Whisper model — using Azure API only
- Mobile or cross-platform — Windows first
- Transcription history/logging — clipboard only
- Voice commands or triggers — push-to-talk only
- Real-time streaming transcription — batch after release
- Meeting transcription — different use case (Otter.ai territory)
- Text editing/refinement — output raw transcription, no AI rewrites

## Context

- User has Azure OpenAI Service with Whisper model deployed
- Target use case: quick voice input while working, paste into any app
- Emphasis on being lightweight — shouldn't feel like it's running
- **Brand guide**: `coxixo-brand-guides.html` — defines visual identity, colors, tray icon design, settings UI mockup

## Constraints

- **Platform**: Windows (initially)
- **API**: Azure OpenAI Whisper (user has existing deployment)
- **UX**: Must feel instant — no loading screens, no heavy UI
- **Memory**: Should be negligible when idle in system tray

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Azure OpenAI over local Whisper | User already has Azure deployment, keeps app lightweight | Good |
| Push-to-talk over toggle | More intuitive for quick input, prevents accidental long recordings | Good |
| Clipboard over file save | Minimal friction, fits voice-to-paste workflow | Good |
| System tray over window | Invisible when not needed, always accessible | Good |
| ApplicationContext pattern | Formless tray app without hidden window | Good |
| WH_KEYBOARD_LL hook | Captures both press and release globally | Good |
| DPAPI with custom entropy | Secure credential storage without external deps | Good |
| WaveInEvent over WaveIn | Better for background apps | Good |
| 500ms minimum duration | Filters accidental taps without being intrusive | Good |
| Programmatic beep generation | Precise frequencies, no external audio files | Good |
| Portuguese hardcoded | Primary use case per user requirements | Good (v1.1: add selection) |
| ProcessCmdKey override | Capture Tab, F-keys in hotkey picker | Good |

---
*Last updated: 2026-02-09 after v1.1 milestone start*
