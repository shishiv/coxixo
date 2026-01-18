# Phase 2: Audio Pipeline - Context

**Gathered:** 2026-01-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Capture audio from default microphone into a memory buffer during push-to-talk. Audio is encoded as 16kHz mono WAV for Whisper API. Provides audio feedback (beeps) for recording start/stop. Handles microphone permission errors gracefully.

</domain>

<decisions>
## Implementation Decisions

### Audio feedback
- Distinct tech sounds (like Discord unmute/mute or walkie-talkie chirps)
- Start: ascending tone, Stop: descending tone (classic walkie-talkie style)
- Volume follows Windows system sounds slider
- Setting available to disable audio feedback (user can rely on visual tray icon only)

### Error notification
- Use Windows toast notifications for microphone problems
- Technical but clear tone: "Microphone access denied. Check Windows privacy settings."
- Actionable toasts: include button to open Windows microphone settings when permission denied

### Recording edge cases
- Minimum duration threshold (~0.5s) to ignore accidental key taps
- Short recordings discarded silently (no notification, no stop beep)

### Claude's Discretion
- Repeat error suppression behavior (show once vs always)
- Mic disconnect during recording handling (save partial vs discard)
- Maximum recording duration (based on Whisper API limits and practical use)
- Exact pitch/frequency values for beep tones
- Warning beep before auto-stop if max duration implemented

</decisions>

<specifics>
## Specific Ideas

- Walkie-talkie feel: ascending chirp to start, descending chirp to stop
- No "cancelled" sound for short recordings — silent discard keeps it non-intrusive

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 02-audio-pipeline*
*Context gathered: 2026-01-18*
