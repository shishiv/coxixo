# 🎤 Coxixo

[![C#](https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![.NET](https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Azure](https://img.shields.io/badge/Azure-0078D4?logo=microsoft-azure&logoColor=white)](https://azure.microsoft.com/)
[![Windows](https://img.shields.io/badge/Windows-0078D6?logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> **Fale. Solte. Cole.** — Transcrição de voz para clipboard em tempo real usando Azure OpenAI Whisper.

![Screenshot](./docs/screenshot.png)

## 📋 Sobre

**Coxixo** é um aplicativo desktop para Windows que transforma fala em texto instantaneamente. Basta pressionar um atalho, falar e soltar — o texto transcrito vai direto para a área de transferência, pronto para colar em qualquer lugar.

Ideal para quem precisa escrever muito (programadores, escritores, estudantes) ou tem dificuldades com digitação. A transcrição usa o modelo **Whisper** da OpenAI via Azure, garantindo alta precisão mesmo com sotaques brasileiros.

**Modelo de uso:** Push-to-talk (aperte para falar, solte para transcrever)

## ✨ Features

- **Push-to-talk intuitivo**: Segure uma tecla, fale, solte → texto na clipboard
- **Transcrição de alta qualidade**: Azure OpenAI Whisper com precisão superior a 95%
- **Suporte a português brasileiro**: Reconhece sotaques regionais e gírias
- **Feedback visual**: Indicador na bandeja do sistema mostra quando está gravando
- **Leve e rápido**: <20MB de memória, transcrição em ~2 segundos
- **Atalho customizável**: Defina a tecla de ativação (padrão: Ctrl + Shift)
- **Histórico local**: Últimas 50 transcrições salvas para consulta
- **Sem telemetria**: Áudio processado via API, nenhum dado armazenado em servidor

## 🛠️ Stack Técnica

**Desktop:**
- **C# 12** — Linguagem de programação
- **Windows Forms** — Interface gráfica leve
- **NAudio** — Captura de áudio do microfone
- **.NET 8** — Runtime moderno

**API:**
- **Azure OpenAI Service** — Whisper API para transcrição
- **HttpClient** — Comunicação assíncrona com a API

**Build:**
- **Visual Studio 2022** — IDE
- **dotnet CLI** — Build e publicação
- **WiX Toolset** — Instalador MSI (opcional)

## 🚀 Como Usar

1. **Baixe o instalador** na [página de releases](https://github.com/shishiv/coxixo/releases)
2. **Configure sua chave de API** da Azure OpenAI no primeiro uso
3. **Defina o atalho** de preferência (padrão: `Ctrl + Shift`)
4. **Use em qualquer lugar:**
   - Segure o atalho
   - Fale naturalmente
   - Solte a tecla
   - Texto aparece na clipboard automaticamente
   - Pressione `Ctrl + V` para colar

**Exemplo prático:**
```
[Segura Ctrl+Shift] "Criar nova função async que busca dados da API" [Solta]
→ Clipboard: "Criar nova função async que busca dados da API"
→ Cola no editor de código
```

## ⚙️ Configuração

No primeiro uso, você precisará:

1. **Criar uma conta Azure** (free tier disponível)
2. **Ativar o serviço OpenAI** no portal Azure
3. **Copiar a chave de API** e o endpoint
4. **Colar no Coxixo** via Settings > API Configuration

**Custo:** ~$0.006 por minuto de áudio transcrito (free tier: $200 de crédito grátis)

## 💻 Como Rodar (Desenvolvimento)

```bash
# Clone o repositório
git clone https://github.com/shishiv/coxixo.git
cd coxixo

# Abra no Visual Studio
start Coxixo.sln

# Ou compile via CLI
dotnet build
dotnet run --project Coxixo
```

**Requisitos:**
- Windows 10/11
- .NET 8 SDK
- Microfone configurado
- Chave de API Azure OpenAI

## 📁 Estrutura do Projeto

```
Coxixo/
├── Forms/               # Janelas da aplicação
│   ├── MainForm.cs     # Tray icon e controles principais
│   └── SettingsForm.cs # Configurações e API key
├── Services/
│   ├── AudioCapture.cs # Captura de áudio via NAudio
│   ├── WhisperAPI.cs   # Integração com Azure OpenAI
│   └── Clipboard.cs    # Gerenciamento da área de transferência
├── Models/
│   └── Transcription.cs # Modelo de dados
├── Utils/
│   ├── Hotkey.cs       # Registro de atalhos globais
│   └── Logger.cs       # Logging local
└── Program.cs          # Entry point
```

## 🔒 Privacidade

- **Áudio não é armazenado**: Processamento em tempo real, descartado após transcrição
- **Chaves locais**: API key salva criptografada no registro do Windows
- **Sem analytics**: Zero coleta de dados de uso
- **Código aberto**: Auditável por qualquer pessoa

## 🐛 Troubleshooting

**Microfone não detectado:**
- Verifique se o microfone está configurado como padrão no Windows
- Vá em Configurações > Privacidade > Microfone e permita acesso ao app

**Transcrição em branco:**
- Verifique sua chave de API no Settings
- Confirme que há créditos na conta Azure
- Teste com áudio mais longo (mínimo 1 segundo)

**Atalho não funciona:**
- Feche outros apps que usam atalhos globais
- Escolha uma combinação diferente no Settings

## 🗺️ Roadmap

- [ ] Suporte a outros idiomas (inglês, espanhol)
- [ ] Modo contínuo (transcrição sem push-to-talk)
- [ ] Integração com modelos locais (Whisper.cpp)
- [ ] Comandos de voz (ex: "ponto final", "nova linha")
- [ ] Exportação de histórico para TXT/CSV

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

**Desenvolvido por [Myke Matos](https://github.com/shishiv)** • Fundador [@TriânguloTEC](https://triangulotec.com.br)
