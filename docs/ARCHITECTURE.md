# 🏗 Architecture Documentation

## Overview

WPFLLM follows a clean **MVVM (Model-View-ViewModel)** architecture with **Dependency Injection** for loose coupling and testability.

## Layer Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                        VIEWS (XAML)                         │
│   MainWindow │ ChatView │ RagView │ SettingsView │ etc.    │
└─────────────────────────┬───────────────────────────────────┘
                          │ Data Binding
┌─────────────────────────▼───────────────────────────────────┐
│                      VIEWMODELS                              │
│   MainViewModel │ ChatViewModel │ RagViewModel │ etc.       │
│   - ObservableObject (CommunityToolkit.Mvvm)                │
│   - RelayCommand for actions                                 │
└─────────────────────────┬───────────────────────────────────┘
                          │ Dependency Injection
┌─────────────────────────▼───────────────────────────────────┐
│                       SERVICES                               │
│   ┌─────────────┐ ┌─────────────┐ ┌──────────────────┐      │
│   │ ChatService │ │  LlmService │ │    RagService    │      │
│   └─────────────┘ └─────────────┘ └──────────────────┘      │
│   ┌─────────────────────┐ ┌─────────────────────────┐       │
│   │ LocalEmbeddingService│ │  ModelDownloadService  │       │
│   └─────────────────────┘ └─────────────────────────┘       │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                        MODELS                                │
│   AppSettings │ Conversation │ Message │ Document │ etc.    │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                    DATA STORAGE                              │
│   SQLite (Dapper) │ Local Files │ ONNX Models               │
└─────────────────────────────────────────────────────────────┘
```

## Key Components

### Services

#### `LlmService`
Handles communication with OpenAI-compatible APIs.

```csharp
public interface ILlmService
{
    IAsyncEnumerable<string> StreamChatAsync(
        List<Message> messages, 
        CancellationToken ct);
    
    Task<float[]> GetEmbeddingAsync(string text);
}
```

**Features:**
- Streaming responses using `IAsyncEnumerable`
- Configurable API endpoint and model
- Embedding generation (API or local)
- Error handling and retry logic

#### `RagService`
Implements Retrieval Augmented Generation pipeline.

```csharp
public interface IRagService
{
    Task<List<string>> GetRelevantChunksAsync(
        string query, 
        int topK = 3);
    
    Task GenerateEmbeddingsAsync(
        IProgress<double> progress);
}
```

**Features:**
- Document chunking with overlap
- Cosine similarity search
- Configurable chunk size and overlap
- Progress reporting

#### `LocalEmbeddingService`
Runs ONNX embedding models locally with Rust FFI tokenizer.

```csharp
public interface ILocalEmbeddingService
{
    Task InitializeAsync(string modelId);
    Task<float[]> GetEmbeddingAsync(string text);
    Task<float[]> GetEmbeddingAsync(string text, bool isQuery);
    Task<bool> IsAvailableAsync();
    int GetDimensions();
}
```

**Features:**
- Lazy model loading
- Thread-safe initialization
- E5 prefix handling (query:/passage:)
- **Rust HuggingFace tokenizer** (add_special_tokens=true)
- Mean pooling + L2 normalization
- Memory-efficient inference

#### `RustTokenizer`
Native Rust FFI wrapper for HuggingFace tokenizers library.

```csharp
public static class RustTokenizer
{
    public static bool Initialize(string tokenizerPath);
    public static int[] Encode(string text, int maxLength = 512);
    public static bool IsInitialized { get; }
}
```

**Why Rust?**
- .NET tokenizer libraries don't support `add_special_tokens=true`
- Without special tokens (`<s>`, `</s>`), E5 embeddings have poor discrimination
- Rust tokenizer improved GAP from **0.7% to 14.5%** (20x better!)

#### `ModelDownloadService`
Manages model downloads from HuggingFace.

```csharp
public interface IModelDownloadService
{
    Task DownloadModelAsync(
        string modelId, 
        IProgress<ModelDownloadProgress> progress,
        CancellationToken ct);
    
    Task<bool> IsModelDownloadedAsync(string modelId);
    Task<ModelDownloadStatus> GetDownloadStatusAsync(string modelId);
}
```

**Features:**
- Resumable downloads
- Progress tracking
- Partial download detection
- Cancellation support

### ViewModels

All ViewModels inherit from `ObservableObject` and use `[RelayCommand]` attributes:

```csharp
public partial class ChatViewModel : ObservableObject
{
    [ObservableProperty]
    private string _userInput = "";
    
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        // Implementation
    }
}
```

### Data Flow

```
User Input → ViewModel Command → Service Call → Data Update → UI Binding
```

## Dependency Injection

Configured in `App.xaml.cs`:

```csharp
private static void ConfigureServices(IServiceCollection services)
{
    // Core services
    services.AddSingleton<IDatabaseService, DatabaseService>();
    services.AddSingleton<ISettingsService, SettingsService>();
    
    // LLM services
    services.AddSingleton<ILlmService, LlmService>();
    services.AddSingleton<IChatService, ChatService>();
    
    // RAG services
    services.AddSingleton<IRagService, RagService>();
    
    // Local embedding services
    services.AddSingleton<ILocalEmbeddingService, LocalEmbeddingService>();
    services.AddSingleton<IModelDownloadService, ModelDownloadService>();
    
    // HTTP client
    services.AddHttpClient();
    
    // ViewModels
    services.AddTransient<MainViewModel>();
    services.AddTransient<ChatViewModel>();
    services.AddTransient<RagViewModel>();
    services.AddTransient<SettingsViewModel>();
    services.AddTransient<EmbeddingsViewModel>();
}
```

## Database Schema

```sql
-- Conversations table
CREATE TABLE Conversations (
    Id TEXT PRIMARY KEY,
    Title TEXT,
    CreatedAt TEXT,
    UpdatedAt TEXT
);

-- Messages table
CREATE TABLE Messages (
    Id TEXT PRIMARY KEY,
    ConversationId TEXT,
    Role TEXT,
    Content TEXT,
    CreatedAt TEXT,
    FOREIGN KEY (ConversationId) REFERENCES Conversations(Id)
);

-- Documents table
CREATE TABLE Documents (
    Id TEXT PRIMARY KEY,
    FileName TEXT,
    Content TEXT,
    ChunkIndex INTEGER,
    Embedding BLOB,
    CreatedAt TEXT
);

-- Settings table
CREATE TABLE Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT
);
```

## External Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| CommunityToolkit.Mvvm | 8.x | MVVM framework |
| Microsoft.Extensions.DependencyInjection | 9.x | DI container |
| Microsoft.Data.Sqlite | 9.x | SQLite provider |
| Dapper | 2.x | Micro-ORM |
| Microsoft.ML.OnnxRuntime | 1.21.0 | ONNX inference |
| Microsoft.SemanticKernel.Connectors.Onnx | 1.55.0-alpha | ONNX embeddings |
| **hf_tokenizer.dll** | - | Rust tokenizer (HuggingFace) |

## Future Improvements

- [ ] Plugin system for custom tools
- [ ] Voice input/output
- [ ] Image understanding (GPT-4V)
- [ ] Conversation export/import
- [ ] Custom themes
- [ ] Multi-language UI
