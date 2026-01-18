# Phase 3: Transcription Loop - Context

**Gathered:** 2026-01-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Complete the core value loop — send audio to Azure Whisper API on hotkey release, receive transcription, copy to clipboard. User can paste transcribed text into any application. This phase delivers the end-to-end transcription functionality. Settings UI and visual polish are separate phases.

</domain>

<decisions>
## Implementation Decisions

### Progress Indication
- Show balloon tip ("Still transcribing...") if API takes longer than 5 seconds
- Visual processing state during API call (Claude decides specifics)

### Success Feedback
- Notification approach on successful transcription (Claude decides: balloon, sound, or silent)
- Empty result handling (Claude decides how to inform user if Whisper returns nothing)

### Error Handling
- Retry strategy for transient failures (Claude decides retry count and backoff)
- Error display method (Claude decides: balloon, icon state, or both)
- Credential validation timing (Claude decides: at startup vs. first use)
- Error message verbosity (Claude decides: user-friendly vs. detailed)

### Claude's Discretion
- Hard timeout value (based on typical API response times)
- Concurrency behavior (block new recording vs. cancel in-flight request)
- Success sound (chirp/ding vs. silent)
- Processing icon animation style
- Empty result notification vs. silent discard
- Technical detail level in error messages

</decisions>

<specifics>
## Specific Ideas

- Keep the walkie-talkie feel established in Phase 2 — quick, responsive feedback
- The "5 second balloon tip" threshold balances user awareness with not being annoying for normal-speed transcriptions

</specifics>

<deferred>
## Deferred Ideas

- Transcription history accessible from context menu — user explicitly wants this but as a future phase (Phase 4 or backlog)

</deferred>

---

*Phase: 03-transcription-loop*
*Context gathered: 2026-01-18*
