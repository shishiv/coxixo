# Feature Landscape: Voice-to-Clipboard Transcription

**Domain:** Windows voice-to-text utility apps
**Researched:** 2026-01-17
**Focus:** Lean utility app (minimal feature set by design)

## Table Stakes

Features users expect from any voice transcription utility. Missing = product feels incomplete or broken.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Push-to-talk hotkey** | Core interaction model; users expect hold-to-record or toggle | Low | Global hotkey registration required |
| **Clipboard output** | Standard way to get text into any app | Low | Windows clipboard API is simple |
| **Visual recording indicator** | Users need to know when they're being recorded | Low | System tray icon change or small overlay |
| **System tray presence** | Minimal footprint expected for utility apps | Low | Standard Windows app pattern |
| **Accurate transcription** | Baseline expectation is 92%+ accuracy; Whisper delivers 95%+ | N/A | Azure OpenAI Whisper handles this |
| **Auto-punctuation** | Modern users expect periods, commas, question marks | Low | Whisper API includes this by default |
| **Error feedback** | Users need to know if transcription failed (no mic, API error) | Low | Toast notification or tray icon state |
| **Microphone selection** | Users may have multiple audio devices | Medium | NAudio/WASAPI device enumeration |

### Table Stakes Rationale

Based on competitor analysis (Easy Dictate, Push-to-Talk, OmniDictate, winWhisper), every lightweight transcription utility includes:
- Global hotkey activation (100% of apps)
- Visual recording state (100% of apps)
- Clipboard or direct paste output (100% of apps)
- System tray/minimal UI (100% of lightweight apps)

**Source:** [Easy Dictate](https://github.com/charleslukowski/easydictate), [Push-to-Talk](https://github.com/yixin0829/push-to-talk)

## Differentiators

Features that set the product apart. Not expected, but valued if present.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Sub-2-second latency** | Feels instant vs sluggish | Medium | Depends on API response time |
| **Hotkey customization** | Let users pick their preferred key combo | Medium | Need hotkey picker UI |
| **Startup with Windows** | Always available without manual launch | Low | Registry or startup folder |
| **Offline fallback** | Works without internet (local Whisper) | High | Would need whisper.cpp integration |
| **Multi-language support** | Whisper supports 99 languages | Low | API parameter; just expose it |
| **Audio feedback sounds** | Beep on start/stop recording | Low | Nice UX polish |
| **Custom prompt/glossary** | Domain-specific terminology | Medium | Requires prompt engineering |

### Differentiator Analysis

For a **lean** app, these are "nice to have later" not "build in v1":
- **High value, low effort:** Audio feedback, startup with Windows, multi-language
- **High value, high effort:** Offline fallback, custom glossary
- **Medium value:** Hotkey customization (most users accept defaults)

## Anti-Features (Keep It Lean)

Features to deliberately NOT build. Common in competitors but add bloat for a utility app.

| Anti-Feature | Why Avoid | What Competitors Do | What Coxixo Should Do |
|--------------|-----------|---------------------|----------------------|
| **Meeting transcription** | Different use case (Otter.ai territory) | Otter, Krisp add speaker ID, timestamps | Focus on quick dictation only |
| **File transcription** | Upload audio files for batch processing | Dragon, Microsoft 365, Sonix | Stay realtime-only |
| **Text refinement/editing** | AI rewrites add complexity and latency | Wispr Flow, winWhisper offer "clean" modes | Output raw transcription |
| **Voice commands** | "Bold that", "new paragraph" parsing | Dragon, Windows Voice Typing | Just transcribe text |
| **In-app text editor** | Full document editing UI | Speechnotes, many web apps | Clipboard-only output |
| **History/transcript storage** | Save past transcriptions | Most meeting apps | No storage = no privacy concerns |
| **Account/login system** | Cloud sync, preferences backup | Wispr Flow, Otter.ai | Local config only |
| **Multiple AI providers** | Choice of Whisper, Deepgram, etc. | Push-to-Talk app | Azure OpenAI Whisper only |
| **Subscription model** | Monthly recurring revenue | Most SaaS competitors | One-time purchase or free |
| **Auto-paste to active window** | Direct keystroke injection | Easy Dictate, OmniDictate | Clipboard only (simpler, more predictable) |
| **Floating widget/overlay** | Persistent recording button on screen | Some apps show widgets | System tray only |
| **Extensive settings UI** | Many configuration pages | Enterprise apps like Dragon | Single settings dialog max |

### Anti-Feature Rationale

User complaints consistently mention:
- "Identity crisis" - apps trying to do too much
- "Can't balance power features with everyday simplicity"
- "We shouldn't have to turn off features just to avoid the pain of using them"
- Preference for one-time purchase over subscription bloat

**Philosophy:** Every feature you don't build is a feature users don't have to learn, configure, or be confused by.

**Source:** [Zapier Best Dictation Software](https://zapier.com/blog/best-text-dictation-software/), [Afading Thought - Dictation App Analysis](https://afadingthought.substack.com/p/best-ai-dictation-tools-for-mac)

## Feature Complexity Matrix

Quick assessment of implementation effort for potential features.

| Feature | Effort | Dependencies | Risk |
|---------|--------|--------------|------|
| Global hotkey registration | Low | Windows API | Low - well-documented |
| System tray app | Low | WPF/WinForms | Low - standard pattern |
| Audio recording | Medium | NAudio/WASAPI | Low - mature library |
| Azure OpenAI API call | Low | HTTP client | Low - REST API |
| Clipboard write | Low | Windows API | Low - trivial |
| Recording indicator overlay | Low | WPF overlay window | Low |
| Settings persistence | Low | JSON file | Low |
| Microphone selection | Medium | NAudio enumeration | Low |
| Hotkey customization UI | Medium | Key picker control | Medium - edge cases |
| Startup with Windows | Low | Registry/startup folder | Low |
| Audio feedback sounds | Low | System.Media | Low |
| Offline mode (local Whisper) | High | whisper.cpp/Whisper.net | High - model download, GPU |
| Multi-provider support | Medium | Abstraction layer | Medium - API differences |

## MVP Recommendation

For MVP, implement table stakes only:

### Must Have (v1.0)
1. Push-to-talk global hotkey (fixed: e.g., Ctrl+Shift+Space)
2. System tray icon with recording state indicator
3. Audio recording via default microphone
4. Azure OpenAI Whisper API transcription
5. Copy result to clipboard
6. Basic error handling (toast or tray notification)

### Defer to Post-MVP
- Hotkey customization (v1.1)
- Microphone selection (v1.1)
- Startup with Windows (v1.1)
- Audio feedback sounds (v1.1)
- Multi-language selection (v1.2)
- Settings dialog (v1.1+)

### Explicitly Out of Scope
- Everything in Anti-Features list
- Offline mode (architectural complexity)
- Multiple transcription providers
- Any form of text editing or refinement

## Competitive Landscape Summary

| App | Model | Price | Key Feature | Why Coxixo is Different |
|-----|-------|-------|-------------|------------------------|
| Easy Dictate | Offline/local | Free | Local Whisper, no cloud | Coxixo uses cloud API (simpler, smaller) |
| Push-to-Talk | Cloud API | Free | Multi-provider | Coxixo is single-provider (simpler) |
| winWhisper | Cloud/Local | Paid | Text refinement modes | Coxixo is raw transcription only |
| Wispr Flow | Cloud | $15/mo | AI formatting, style memory | Coxixo is utility, not writing assistant |
| Windows Voice Typing | Free/Built-in | Free | Works everywhere | Coxixo is clipboard-focused, one-click |

**Coxixo's niche:** The simplest possible voice-to-clipboard utility. No accounts, no subscriptions, no AI rewrites. Just hold hotkey, speak, release, paste.

## Sources

- [Zapier - Best Dictation Software 2026](https://zapier.com/blog/best-text-dictation-software/)
- [TechCrunch - Best AI Dictation Apps 2025](https://techcrunch.com/2025/12/30/the-best-ai-powered-dictation-apps-of-2025/)
- [GitHub - Easy Dictate](https://github.com/charleslukowski/easydictate)
- [GitHub - Push-to-Talk](https://github.com/yixin0829/push-to-talk)
- [Microsoft - Windows Voice Typing](https://support.microsoft.com/en-us/windows/use-voice-typing-to-talk-instead-of-type-on-your-pc-fec94565-c4bd-329d-e59a-af033fa5689f)
- [StarWhisper - Whisper Desktop App](https://starwhisper.ai/landing/whisper-desktop-app.html)
- [winWhisper](https://www.winwhisper.app/)
