<div align="center">

# 🤖 WPFLLM

### Intelligent Desktop Chat Application with RAG & Local Embeddings

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Desktop-0078D4?style=for-the-badge&logo=windows&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![OpenAI](https://img.shields.io/badge/OpenAI-Compatible-412991?style=for-the-badge&logo=openai&logoColor=white)](https://openai.com/)

*A modern WPF desktop application for AI-powered conversations with RAG (Retrieval Augmented Generation) support and local embedding models.*

[Features](#-features) • [Architecture](#-architecture) • [Installation](#-installation) • [Usage](#-usage) • [Contributing](#-contributing)

</div>

---

## ✨ Features

### 💬 **Intelligent Chat**
- Real-time streaming responses from LLMs
- Multiple conversation management with history
- Markdown rendering support
- Token usage tracking

### 📚 **RAG (Retrieval Augmented Generation)**
- Import documents (.txt, .md, .json, .csv)
- Semantic search through your knowledge base
- Context-aware responses based on your documents
- Chunking and embedding generation

### 🧠 **Local Embedding Models**
- Download and run embedding models locally
- Support for multilingual E5 models (Small/Base/Large)
- No API costs for embeddings
- Privacy-first: your data stays on your machine

### 🎨 **Modern UI/UX**
- Clean dark theme with accent colors
- Responsive tab-based navigation
- Visual model quality ratings (★★★★★)
- Hardware requirement indicators
- Progress tracking for downloads

### 🔒 **Privacy & Security**
- Encrypted local SQLite database
- No telemetry or tracking
- API keys stored securely
- Offline embedding generation

---

## 🏗 Architecture

```
WPFLLM/
├── 📁 Models/           # Data models and settings
├── 📁 Services/         # Business logic layer
│   ├── ChatService      # Conversation management
│   ├── LlmService       # LLM API integration
│   ├── RagService       # RAG pipeline
│   ├── LocalEmbedding   # ONNX embedding generation
│   └── ModelDownload    # HuggingFace model downloads
├── 📁 ViewModels/       # MVVM ViewModels
├── 📁 Views/            # WPF XAML views
├── 📁 Converters/       # Value converters
└── 📁 Themes/           # UI theming
```

### Design Patterns
- **MVVM** - Clean separation of concerns
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection
- **Repository Pattern** - SQLite data access
- **Service Layer** - Encapsulated business logic

### Tech Stack
| Technology | Purpose |
|------------|---------|
| .NET 10.0 | Runtime |
| WPF | UI Framework |
| CommunityToolkit.Mvvm | MVVM implementation |
| SQLite + Dapper | Local database |
| Semantic Kernel | AI orchestration |
| ONNX Runtime | Local model inference |

---

## 📦 Installation

### Prerequisites
- Windows 10/11
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- ~2GB disk space for embedding models (optional)

### Quick Start

```bash
# Clone the repository
git clone https://github.com/DamianTarnowski/WPFLLM.git
cd WPFLLM

# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

### Build Release

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

---

## 🚀 Usage

### 1. Configure API
1. Open **Settings** tab
2. Enter your OpenAI API key (or compatible provider)
3. Select model (e.g., `gpt-4o-mini`, `gpt-4o`)
4. Optionally change endpoint for other providers

### 2. Start Chatting
1. Go to **Chat** tab
2. Create a new conversation or select existing
3. Type your message and press Enter

### 3. Set Up RAG (Optional)
1. Open **Knowledge Base** tab
2. Add documents to your knowledge base
3. Generate embeddings
4. Enable RAG in Settings

### 4. Local Embeddings (Optional)
1. Open **Embeddings** tab
2. Download a model (E5 Small recommended for start)
3. Load the model
4. Run similarity tests to verify quality

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

## 🛠 Development

### Project Structure
```csharp
// Dependency Injection setup in App.xaml.cs
services.AddSingleton<IDatabaseService, DatabaseService>();
services.AddSingleton<ILlmService, LlmService>();
services.AddSingleton<IRagService, RagService>();
services.AddSingleton<ILocalEmbeddingService, LocalEmbeddingService>();
services.AddHttpClient();
```

### Key Classes
- `LlmService` - OpenAI API integration with streaming
- `RagService` - Document chunking and retrieval
- `LocalEmbeddingService` - ONNX model loading and inference
- `ModelDownloadService` - Resumable downloads from HuggingFace

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
