# Phase 4: Polish - Context

**Gathered:** 2026-01-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Apply brand visual identity to tray icons and build a settings UI for configuration. The brand guide (`coxixo-brand-guides.html`) establishes colors, logo design, and UI mockup. This phase implements those designs and adds the necessary UX behaviors.

</domain>

<decisions>
## Implementation Decisions

### Brand Guide Reference
The brand guide provides comprehensive visual direction:
- **Colors:** #0078D4 (Azure Blue), #2D3748 (Slate Dark), #00CC6A (Success Green), #1A1A1A (Background Dark)
- **Logo:** 3 sound bars forming "C" shape with green accent dot
- **Tray icon:** White/gray when idle, red with pulsing dot when recording
- **Settings mockup:** Dark theme, status indicator, hotkey display, credential fields, Save button

### Claude's Discretion

User granted full discretion on all behavioral decisions. Claude should make choices that:
- Align with the minimal-friction goal ("hold, speak, release, paste")
- Follow Windows UX conventions where appropriate
- Keep implementation simple while maintaining polish

**Tray Icon States:**
- Whether to show distinct "processing" state while API call in progress
- How to indicate error state (credential issues, API unreachable)
- How to show "not configured" state when credentials missing
- Recording animation details (full red icon vs. just pulsing dot)

**Settings Window Behavior:**
- How to open (double-click tray, right-click menu, or both)
- Hotkey picker implementation (click-to-record vs. dropdown)
- API connection testing approach (manual button vs. auto-test on save vs. live indicator)
- Save behavior (explicit Save button as shown in mockup is likely choice)

**Notifications & Feedback:**
- Whether to show notification after successful transcription (beep may be enough)
- Error notification style (balloon tips vs. custom toasts)
- Error message detail level (user-friendly vs. technical)
- Feedback for too-short recordings (current behavior: silent discard)
- Notification styling (standard Windows vs. custom Coxixo-branded)

**Context Menu Design:**
- Menu items to include (Settings, About, Exit as minimum)
- Whether to include About dialog or just show version in Settings footer
- Whether to include Pause/Resume toggle for temporarily disabling hotkey
- Whether to include "Start with Windows" option and where to place it

</decisions>

<specifics>
## Specific Ideas

- Brand guide mockup shows "Whisper API Connected" with "Latency: 120ms" - this establishes the status indicator pattern
- Mockup shows "View Logs" link in Settings footer - consider including
- Slogan "Fale. Solte. Cole." (Speak. Release. Paste.) captures the UX intent
- The mockup uses Segoe UI / Inter fonts

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope

</deferred>

---

*Phase: 04-polish*
*Context gathered: 2026-01-18*
