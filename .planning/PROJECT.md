# Coxixo

## What This Is

A lean Windows system tray app for voice-to-clipboard transcription. Push-to-talk with a global hotkey captures speech, sends it to Azure OpenAI Whisper, and puts the transcribed text directly in the clipboard — ready to paste anywhere.

## Core Value

Frictionless voice input: hold a key, speak, release, paste. Nothing else.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] System tray presence with minimal footprint
- [ ] Global push-to-talk hotkey (hold to record, release to transcribe)
- [ ] Audio capture from default microphone
- [ ] Azure OpenAI Whisper API integration for transcription
- [ ] Transcription result copied to clipboard automatically
- [ ] Audio feedback (beep on start/stop recording)
- [ ] Visual feedback (tray icon changes while recording)
- [ ] Minimal settings UI (API key, hotkey configuration)
- [ ] Portuguese BR as initial language
- [ ] Fast startup and low memory usage

### Out of Scope

- [ ] Local Whisper model — using Azure API only
- [ ] Mobile or cross-platform — Windows first
- [ ] Transcription history/logging — clipboard only for v1
- [ ] Voice commands or triggers — push-to-talk only
- [ ] Real-time streaming transcription — batch after release

## Context

- User has Azure OpenAI Service with Whisper model deployed
- Target use case: quick voice input while working, paste into any app
- Emphasis on being lightweight — shouldn't feel like it's running
- Future expansion: multi-language support, auto-detection
- **Brand guide**: `coxixo-brand-guides.html` — defines visual identity, colors, tray icon design, settings UI mockup
- **Slogan**: "Fale. Solte. Cole."

## Constraints

- **Platform**: Windows (initially)
- **API**: Azure OpenAI Whisper (user has existing deployment)
- **UX**: Must feel instant — no loading screens, no heavy UI
- **Memory**: Should be negligible when idle in system tray

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Azure OpenAI over local Whisper | User already has Azure deployment, keeps app lightweight | — Pending |
| Push-to-talk over toggle | More intuitive for quick input, prevents accidental long recordings | — Pending |
| Clipboard over file save | Minimal friction, fits voice-to-paste workflow | — Pending |
| System tray over window | Invisible when not needed, always accessible | — Pending |

---
*Last updated: 2026-01-17 after initialization*
