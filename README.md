<div align="center">

# 🤖 WPFLLM

### AI Assistant with RAG, Document Analysis & Enterprise Security

[![Microsoft Store](https://img.shields.io/badge/Microsoft%20Store-Available-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)](https://apps.microsoft.com/store/detail/24677DamianTarnowski.WPFLLM-AIAssistant)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Desktop-0078D4?style=for-the-badge&logo=windows&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![Tests](https://img.shields.io/badge/Tests-317%20passed-success?style=for-the-badge)](WPFLLM.Tests/)
[![OpenAI](https://img.shields.io/badge/OpenAI-Compatible-412991?style=for-the-badge&logo=openai&logoColor=white)](https://openai.com/)

*A modern WPF desktop application for AI-powered conversations with advanced RAG, document analysis, and enterprise-grade security. Built with .NET 10, featuring offline-first design, local embedding models, and hybrid retrieval.*

[Features](#-features) • [Quick Start](#-quick-start) • [Architecture](#-architecture) • [Documentation](#-documentation) • [Testing](#-testing) • [Contributing](#-contributing)

---

<img src="docs/assets/screenshot-chat.png" alt="WPFLLM Chat Interface" width="800">

*Screenshot: Chat interface with RAG debug panel showing retrieval metrics*

</div>

---

## ✨ Features

### 💬 **Intelligent Chat**
- Real-time streaming responses from LLMs
- Multiple conversation management with history
- Markdown rendering support
- RAG Debug Panel with sources, scores, and latency metrics

### 📚 **Advanced RAG System**
- **Hybrid Retrieval**: Vector search + FTS5 keyword search with RRF fusion
- Import documents (.txt, .md, .json, .csv, .pdf, .docx)
- Configurable TopK, similarity threshold, retrieval mode
- Full pipeline transparency with Flight Recorder

### ✈️ **RAG Debug Panel (Flight Recorder)**
The "black box" for your RAG pipeline - understand exactly why the model responded the way it did:

| Tab | What it shows |
|-----|---------------|
| **📄 Sources** | Selected chunks with Vec/KW/RRF scores and content preview |
| **📊 Candidates** | All evaluated chunks (not just selected), with ranking and token counts |
| **⏱️ Timings** | Visual breakdown: EmbedQuery, VectorSearch, KeywordSearch, Merge+Rerank |

**Summary Cards** show at a glance:
- Total pipeline time (ms)
- Selected/Total chunks ratio
- Vector matches count
- Keyword matches count

```
Formula: RRF(k=60) - Reciprocal Rank Fusion combining vector and keyword results
```

### 📄 **Document Analysis Mode**
- Analyze documents and transcripts for insights
- **Summary**: TL;DR of content
- **Detected Intents**: Customer/user intentions with confidence scores
- **Red Flags**: Risk detection with severity levels
- **Compliance Checklist**: Automatic verification of required items
- **Suggested Response**: Draft professional replies

### 🧠 **Local Embedding Models**
- Download and run embedding models locally (ONNX)
- Support for multilingual E5 models (Small/Base/Large/Instruct)
- No API costs for embeddings
- Privacy-first: your data stays on your machine

### 🎨 **Modern UI/UX**
- Clean dark theme with accent colors
- **Multi-language support**: English, Polski (more coming)
- Status bar with offline/encryption/network indicators
- RAG Debug panel with professional metrics display

### 🔒 **Enterprise Security**
- **AES-256-GCM encryption** for data at rest
- **DPAPI key protection** tied to Windows user
- Offline mode indicator
- Network calls counter (transparency)
- Zero telemetry, zero data leakage

---

## 🏗 Architecture

*Architecture inspired by RAG frameworks like LlamaIndex, implemented natively in .NET for offline mode.*

```
┌─────────────────────────────────────────────────────────────────┐
│                         UI Layer (WPF)                          │
│  ┌─────────┐ ┌─────────┐ ┌──────────┐ ┌─────────┐ ┌──────────┐ │
│  │  Chat   │ │Knowledge│ │ Document │ │Embeddings│ │ Settings │ │
│  │  View   │ │  Base   │ │ Analysis │ │   View   │ │   View   │ │
│  └────┬────┘ └────┬────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘ │
│       │           │           │            │            │       │
│  ┌────┴───────────┴───────────┴────────────┴────────────┴────┐  │
│  │                    ViewModels (MVVM)                       │  │
│  │   IAsyncEnumerable<string> for streaming responses         │  │
│  │   Update UI via Dispatcher (only in UI layer)              │  │
│  └────────────────────────────┬───────────────────────────────┘  │
└───────────────────────────────┼──────────────────────────────────┘
                                │
┌───────────────────────────────┼──────────────────────────────────┐
│                    Application Layer                             │
│  ┌────────────────┐ ┌─────────────────┐ ┌──────────────────────┐ │
│  │ ChatOrchestrator│ │IngestionPipeline│ │  RagQueryEngine     │ │
│  │ (ChatService)   │ │ (Channel<T>)    │ │  (Hybrid Retrieval) │ │
│  └────────────────┘ └─────────────────┘ └──────────────────────┘ │
│  ┌────────────────┐ ┌─────────────────┐ ┌──────────────────────┐ │
│  │DocumentAnalysis│ │ StatusService   │ │ LocalizationService │ │
│  └────────────────┘ └─────────────────┘ └──────────────────────┘ │
└───────────────────────────────┬──────────────────────────────────┘
                                │
┌───────────────────────────────┼──────────────────────────────────┐
│                       Core Interfaces                            │
│  IEmbedder │ IVectorIndex │ IRetriever │ ILLM │ IEncryptor       │
└───────────────────────────────┬──────────────────────────────────┘
                                │
┌───────────────────────────────┼──────────────────────────────────┐
│                    Infrastructure Layer                          │
│  ┌─────────────────┐ ┌─────────────────┐ ┌────────────────────┐  │
│  │ SQLite + FTS5   │ │  ONNX Runtime   │ │  DPAPI + AES-GCM   │  │
│  │ (vector + text) │ │  (CPU/DirectML) │ │  (encryption)      │  │
│  └─────────────────┘ └─────────────────┘ └────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

### Key Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| **Hybrid Retrieval (FTS5 + Vectors)** | Combines keyword precision with semantic understanding via RRF fusion |
| **Channel\<T\> Ingestion Queue** | Backpressure, cancellation, retry - enterprise-grade document processing |
| **DPAPI + AES-256-GCM** | Windows-native key protection, industry-standard encryption |
| **Offline-First Design** | Data never leaves device, zero external dependencies for core features |
| **IAsyncEnumerable Streaming** | Token-by-token response delivery without blocking UI |

### Tech Stack
| Technology | Purpose |
|------------|---------|
| .NET 10.0 | Runtime |
| WPF | UI Framework |
| CommunityToolkit.Mvvm | MVVM implementation |
| SQLite + FTS5 | Local database with full-text search |
| ONNX Runtime | Local model inference |
| AES-GCM + DPAPI | Enterprise encryption |

---

## 🚀 Quick Start

### Prerequisites
- **Windows 10/11** (64-bit)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- ~2GB disk space for embedding models (optional)

### Installation

**Option 1: Microsoft Store (Recommended)**

[<img src="https://get.microsoft.com/images/en-us%20dark.svg" alt="Download from Microsoft Store" height="80"/>](https://apps.microsoft.com/store/detail/24677DamianTarnowski.WPFLLM-AIAssistant)

**Option 2: Build from Source**

```bash
# Clone the repository
git clone https://github.com/DamianTarnowski/WPFLLM.git
cd WPFLLM

# Build and run
dotnet run
```

### First Run Setup

1. **Configure API** → Settings → Enter OpenAI/OpenRouter API key
2. **Start Chatting** → Chat tab → Create conversation → Send message
3. **Enable RAG** *(optional)* → Knowledge Base → Add documents → Generate embeddings
4. **Go Offline** *(optional)* → Embeddings → Download local model

### Build for Production

```bash
# Self-contained executable
dotnet publish -c Release -r win-x64 --self-contained -o ./publish

# Framework-dependent (smaller)
dotnet publish -c Release -r win-x64 -o ./publish
```

---

## 🔧 Configuration

### Data Location
```
%LOCALAPPDATA%\WPFLLM\
├── wpfllm.db           # Encrypted database
└── models/             # Downloaded embedding models
    ├── multilingual-e5-small/
    ├── multilingual-e5-base/
    └── multilingual-e5-large/
```

### Supported API Providers
| Provider | Endpoint |
|----------|----------|
| OpenAI | `https://api.openai.com/v1` |
| Azure OpenAI | `https://{resource}.openai.azure.com/` |
| OpenRouter | `https://openrouter.ai/api/v1` |
| Ollama | `http://localhost:11434/v1` |
| LM Studio | `http://localhost:1234/v1` |

---

## 📊 Embedding Models

| Model | Dimensions | Size | Quality | RAM |
|-------|------------|------|---------|-----|
| E5 Small | 384 | ~470MB | ★★★☆☆ | 1-2 GB |
| E5 Base | 768 | ~1.1GB | ★★★★☆ | 2-3 GB |
| E5 Large | 1024 | ~2.2GB | ★★★★★ | 4-6 GB |

All models support **100+ languages** including Polish, English, German, French, and more.

---

## 📖 Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/ARCHITECTURE.md) | System design, MVVM patterns, service layer |
| [Embeddings](docs/EMBEDDINGS.md) | Local embedding models, Rust tokenizer, E5 setup |
| [Store Publishing](WPFLLM.Package/STORE_SUBMISSION_GUIDE.md) | Microsoft Store submission guide |
| [Privacy Policy](PRIVACY.md) | Privacy policy for Store compliance |
| [Contributing](CONTRIBUTING.md) | How to contribute, code style, commit conventions |
| [Changelog](CHANGELOG.md) | Version history and release notes |

### Key Services

| Service | Responsibility |
|---------|---------------|
| `LlmService` | OpenAI-compatible API, streaming, embeddings |
| `RagService` | Document chunking, hybrid retrieval (FTS5 + vectors) |
| `ChatService` | Conversation management, message history |
| `LocalEmbeddingService` | ONNX model inference, Rust tokenizer |
| `EncryptionService` | AES-256-GCM encryption, DPAPI key protection |
| `IngestionService` | Background document processing queue |

---

## 🧪 Testing

The project includes **317 tests** with comprehensive coverage:

| Category | Tests | Description |
|----------|-------|-------------|
| Unit Tests | 184 | Services, models, utilities |
| Integration Tests | 120 | Database, RAG, chat workflows |
| Real API Tests | 13 | Live OpenRouter API validation |

### Running Tests

```bash
# All tests (mocked, no API key needed)
dotnet test

# With verbose output
dotnet test --logger "console;verbosity=normal"

# Only real API tests
dotnet test --filter "TestCategory=RealApi"
```

### Real API Tests

To run tests against the real OpenRouter API:

```bash
# Option 1: Environment variable
$env:OPENROUTER_API_KEY = "sk-or-v1-your-key-here"
dotnet test --filter "TestCategory=RealApi"

# Option 2: Create WPFLLM.Tests/.env file
echo "OPENROUTER_API_KEY=sk-or-v1-your-key-here" > WPFLLM.Tests/.env
dotnet test --filter "TestCategory=RealApi"
```

> **Note**: The `.env` file is gitignored. Tests are automatically skipped if no API key is found.

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👤 Author

**Damian Tarnowski**

- GitHub: [@DamianTarnowski](https://github.com/DamianTarnowski)

---

<div align="center">

Made with ❤️ and .NET

</div>
