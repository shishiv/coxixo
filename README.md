# 🎤 Coxixo

Push-to-talk voice transcription for Windows — speak, release, paste. Powered by Azure OpenAI Whisper.

## ✨ Features

- **Push-to-talk interface** — Hold hotkey, speak, release → instant clipboard paste
- **High-quality transcription** — Azure OpenAI Whisper with 95%+ accuracy
- **Brazilian Portuguese support** — Handles regional accents and colloquialisms
- **System tray integration** — Lightweight, always-ready background service
- **Custom hotkeys** — Configure your preferred activation shortcut
- **Local history** — Last 50 transcriptions saved for reference
- **Privacy-first** — Audio processed in real-time, not stored
- **Fast & lightweight** — <20MB memory, ~2s transcription time

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| **Language** | C# 12 |
| **Framework** | .NET 8 |
| **UI** | Windows Forms / WPF |
| **Audio** | NAudio library |
| **Transcription** | Azure OpenAI Whisper API |
| **Build** | Visual Studio 2022, dotnet CLI |

## 🗺️ Roadmap

- [x] **Phase 1: Windows Desktop** — Push-to-talk, Azure Whisper, clipboard integration
- [ ] **Phase 2: Enhanced UX** 🚧
  - [ ] Custom hotkey configuration UI
  - [ ] Audio waveform preview during recording
  - [ ] Transcription history panel with search
  - [ ] Multi-language support (English, Spanish)
- [ ] **Phase 3: Electron Migration**
  - [ ] Cross-platform: Windows, macOS, Linux
  - [ ] Modern UI with React/Tailwind
  - [ ] Native system integration per OS
  - [ ] Auto-updates
- [ ] **Phase 4: AI Features**
  - [ ] Real-time transcription (continuous mode)
  - [ ] Speaker diarization
  - [ ] Auto-summary generation
  - [ ] Translation to multiple languages
- [ ] **Phase 5: Integrations**
  - [ ] Notion, Obsidian, Roam sync
  - [ ] Slack direct messaging
  - [ ] VS Code extension
  - [ ] REST API for automation
- [ ] **Phase 6: Cloud & Collaboration**
  - [ ] Cloud sync for history
  - [ ] Shared transcriptions
  - [ ] Team workspaces
  - [ ] Local Whisper.cpp support (offline mode)

## 🚀 Getting Started

### Installation

1. Download the installer from [Releases](https://github.com/shishiv/coxixo/releases)
2. Run setup and configure your Azure OpenAI API key
3. Set your preferred hotkey (default: `Ctrl + Shift`)

### Usage

1. Hold your hotkey
2. Speak naturally
3. Release the key
4. Text appears in clipboard automatically
5. Paste anywhere with `Ctrl + V`

### Development

```bash
# Clone repository
git clone https://github.com/shishiv/coxixo.git
cd coxixo

# Build and run
dotnet build
dotnet run --project Coxixo
```

**Requirements:**
- Windows 10/11
- .NET 8 SDK
- Azure OpenAI API key
- Microphone

## 🔒 Privacy

- Audio processed in real-time, not stored
- API keys encrypted locally
- No telemetry or usage tracking
- Open source and auditable

## 📄 License

MIT

---

**Built by Myke Matos — TriânguloTEC**
