<h1 align="center">
  <br>
  <img src="https://img.shields.io/badge/Coxixo-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="Coxixo" width="200">
  <br>
  Coxixo
  <br>
</h1>

<h3 align="center">🎙️ Fale. Solte. Cole.</h3>

<p align="center">
  <strong>Transcrição de voz para área de transferência no Windows</strong><br>
  Segure uma tecla, fale, solte — seu texto está no Ctrl+V.
</p>

<p align="center">
  <a href="#-sobre">Sobre</a> •
  <a href="#-como-funciona">Como Funciona</a> •
  <a href="#-instalação">Instalação</a> •
  <a href="#-configuração">Configuração</a> •
  <a href="#-tecnologias">Tecnologias</a> •
  <a href="#-roadmap">Roadmap</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-1.0-blue?style=flat-square" alt="Version">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 8">
  <img src="https://img.shields.io/badge/Azure-OpenAI-0078D4?style=flat-square&logo=microsoftazure" alt="Azure OpenAI">
  <img src="https://img.shields.io/badge/platform-Windows-0078D6?style=flat-square&logo=windows" alt="Windows">
</p>

---

## 💡 Sobre

**Coxixo** (do verbo "coxixar" — falar baixinho, sussurrar) é um app minimalista que vive na bandeja do sistema do Windows. Ele transforma sua voz em texto usando o Azure OpenAI Whisper e coloca o resultado direto na área de transferência.

**Sem janelas. Sem distrações. Só fale e cole.**

### Por que usar?

- 🚀 **Rápido** — Segure F8, fale, solte. Pronto.
- 🎯 **Focado** — Faz uma coisa só, e faz bem feito
- 🔒 **Seguro** — Credenciais criptografadas com DPAPI do Windows
- 🪶 **Leve** — ~1.700 linhas de C#, consumo mínimo de memória
- 🎨 **Bonito** — Ícones animados e tema dark na configuração

---

## 🔄 Como Funciona

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│   1. SEGURE F8        2. FALE           3. SOLTE            │
│   ┌──────────┐       ┌──────────┐      ┌──────────┐        │
│   │  🎙️ bip  │  ───► │ "Olá..." │ ───► │  🎙️ bip  │        │
│   │ (início) │       │          │      │  (fim)   │        │
│   └──────────┘       └──────────┘      └──────────┘        │
│                                              │              │
│                                              ▼              │
│                                   ┌──────────────────┐     │
│   4. COLE (Ctrl+V)                │  Azure Whisper   │     │
│   ┌──────────────┐                │  ☁️ Transcrição   │     │
│   │ "Olá mundo!" │ ◄───────────── │                  │     │
│   └──────────────┘                └──────────────────┘     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Fluxo completo:**

1. **Segure** a hotkey (padrão: F8) — você ouve um bip ascendente 🔊
2. **Fale** o que quiser enquanto segura a tecla
3. **Solte** a tecla — você ouve um bip descendente 🔊
4. O áudio é enviado ao Azure OpenAI Whisper
5. A transcrição vai direto para a área de transferência
6. **Cole** (Ctrl+V) em qualquer lugar!

---

## 📦 Instalação

### Pré-requisitos

- Windows 10/11
- .NET 8.0 Runtime ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Conta Azure com Azure OpenAI Service
- Modelo Whisper implantado no Azure OpenAI

### Download

1. Baixe a última release em [Releases](https://github.com/shishiv/coxixo/releases)
2. Extraia o ZIP
3. Execute `Coxixo.exe`

### Build do código-fonte

```bash
git clone https://github.com/shishiv/coxixo.git
cd coxixo
dotnet build -c Release
```

---

## ⚙️ Configuração

Na primeira execução, clique com o botão direito no ícone da bandeja e selecione **Settings**.

### Campos obrigatórios

| Campo | Descrição | Exemplo |
|-------|-----------|---------|
| **Azure Endpoint** | URL do seu recurso Azure OpenAI | `https://seu-recurso.openai.azure.com/` |
| **API Key** | Chave de API do Azure OpenAI | `xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx` |
| **Whisper Deployment** | Nome do deployment do Whisper | `whisper` |
| **Hotkey** | Tecla para push-to-talk | `F8`, `Home`, `PageUp` |

### Onde encontrar as credenciais Azure

1. Acesse o [Portal Azure](https://portal.azure.com)
2. Vá em **Azure OpenAI Service** → seu recurso
3. Em **Keys and Endpoint**, copie a chave e o endpoint
4. Em **Model deployments**, verifique o nome do seu deployment Whisper

---

## 🎨 Interface

### Ícone na Bandeja

| Estado | Ícone | Descrição |
|--------|-------|-----------|
| **Ocioso** | 🔘 Barras cinzas + ponto verde | Pronto para gravar |
| **Gravando** | 🔴 Barras vermelhas pulsando | Capturando áudio |

### Janela de Configurações

<table>
<tr>
<td>

**Tema dark minimalista**
- Fundo: `#1E1E1E`
- Superfície: `#252526`
- Destaque: `#0078D4` (Azure Blue)

</td>
<td>

**Indicador de conexão**
- 🟢 Verde: API conectada + latência
- 🔴 Vermelho: Erro de conexão

</td>
</tr>
</table>

---

## 🛠️ Tecnologias

| Componente | Tecnologia |
|------------|------------|
| **Framework** | .NET 8 WinForms |
| **Áudio** | NAudio 2.2.1 |
| **API** | Azure.AI.OpenAI 2.1.0 |
| **Segurança** | Windows DPAPI |
| **Ícones** | System.Drawing (gerados programaticamente) |

### Arquitetura

```
Coxixo/
├── Program.cs                    # Entry point + single instance
├── TrayApplicationContext.cs     # ApplicationContext principal
├── Forms/
│   └── SettingsForm.cs          # UI de configurações
├── Services/
│   ├── KeyboardHookService.cs   # WH_KEYBOARD_LL hook
│   ├── AudioCaptureService.cs   # NAudio microphone capture
│   ├── AudioFeedbackService.cs  # Beeps walkie-talkie
│   ├── TranscriptionService.cs  # Azure Whisper client
│   ├── ConfigurationService.cs  # JSON settings
│   └── CredentialService.cs     # DPAPI encryption
├── Models/
│   └── AppSettings.cs           # Configurações tipadas
└── Resources/
    ├── icon-idle.ico            # Ícone ocioso
    ├── icon-recording.ico       # Ícone gravando
    ├── icon-recording-pulse.ico # Ícone gravando (pulso)
    ├── beep-start.wav           # Som início
    └── beep-stop.wav            # Som fim
```

---

## 🗺️ Roadmap

### ✅ v1.0 MVP (atual)

- [x] Push-to-talk com hotkey global
- [x] Captura de áudio 16kHz mono WAV
- [x] Integração Azure OpenAI Whisper
- [x] Clipboard automático
- [x] Feedback sonoro (walkie-talkie)
- [x] Ícones com animação
- [x] Settings UI com tema dark
- [x] Credenciais criptografadas (DPAPI)

### 🔜 v1.1 (próxima)

- [ ] Suporte a modificadores na hotkey (Ctrl+X, Shift+Y)
- [ ] Seleção de microfone
- [ ] Seleção de idioma (PT, EN, auto-detect)
- [ ] Iniciar com o Windows
- [ ] Transcrições recentes no menu

### 💭 Futuro

- [ ] Múltiplos providers de transcrição
- [ ] Overlay minimalista durante gravação
- [ ] Histórico de transcrições (opcional)

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Por favor:

1. Faça um fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/nova-feature`)
3. Commit suas mudanças (`git commit -m 'feat: adiciona nova feature'`)
4. Push para a branch (`git push origin feature/nova-feature`)
5. Abra um Pull Request

---

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## 🙏 Agradecimentos

- [Azure OpenAI Service](https://azure.microsoft.com/products/ai-services/openai-service) pela API Whisper
- [NAudio](https://github.com/naudio/NAudio) pela biblioteca de áudio
- Comunidade .NET brasileira

---

<p align="center">
  <sub>Feito com ❤️ para o Meetup de AI</sub>
</p>

<p align="center">
  <strong>Coxixo</strong> — Porque às vezes, um sussurro vale mais que mil teclas.
</p>
