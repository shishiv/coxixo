# 🎤 Coxixo

Coxixo transcribes your voice to text on Windows—just speak, release, and paste. It's powered by Azure OpenAI Whisper.

## ✨ Features

- **Push-to-talk interface** — Hold the hotkey, speak, and release. Boom—instant clipboard paste.
- **High-quality transcription** — Azure OpenAI Whisper? It's got 95%+ accuracy.
- **Brazilian Portuguese support** — It even gets regional accents and slang.
- **System tray integration** — A lightweight background service that's always ready.
- **Custom hotkeys** — Pick your go-to activation shortcut.
- **Local history** — Your last 50 transcriptions are saved.
- **Privacy-first** — Audio's processed instantly, and never saved.
- **Fast & lightweight** — Under 20MB memory, ~2s transcription time

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

- [x] **Phase 1: Windows Desktop** — Push-to-talk, Azure Whisper, clipboard
- [ ] **Phase 2: Enhanced UX** 🚧
  - [ ] Custom hotkey config UI
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

1.  Grab the installer from [Releases](https://github.com/shishiv/coxixo/releases)
2.  Run setup and add your Azure OpenAI API key
3.  Set a hotkey (default: `Ctrl + Shift`)

### Usage

1.  Hold your hotkey
2.  Speak normally
3.  Release
4.  Text's automatically on your clipboard
5.  Paste anywhere with `Ctrl + V`

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

- Audio processed live, never stored
- API keys encrypted locally
- No telemetry or tracking
- Open source

## 📄 License

MIT

---

**Built by Myke Matos — TriânguloTEC**

