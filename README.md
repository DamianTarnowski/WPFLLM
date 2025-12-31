# WPFLLM - Local LLM Chat Application

A simple WPF desktop application for chatting with LLMs via API, with RAG (Retrieval Augmented Generation) support.

## Features

- **Chat with LLMs** - Supports OpenAI-compatible APIs with streaming responses
- **Multiple Conversations** - Create, manage, and switch between chat sessions
- **RAG Support** - Add documents to knowledge base for context-aware responses
- **Local Storage** - All data stored locally in encrypted SQLite database
- **Modern UI** - Dark theme with clean, modern design

## Requirements

- .NET 10.0 SDK
- Windows 10/11

## Getting Started

1. Clone or download the project
2. Open in Visual Studio 2022 or run from command line:
   ```bash
   dotnet restore
   dotnet run
   ```
3. Go to **Settings** tab and configure:
   - **API Key** - Your OpenAI API key (or compatible provider)
   - **API Endpoint** - Default is OpenAI, change for other providers
   - **Model** - e.g., `gpt-4o-mini`, `gpt-4o`, `gpt-3.5-turbo`

## Using RAG

1. Go to **Knowledge Base** tab
2. Click **Add Documents** to import text files (.txt, .md, .json, .csv)
3. Click **Generate Embeddings** to process documents
4. Enable RAG in **Settings** tab
5. Your chats will now include relevant context from your documents

## Configuration

Settings are stored in:
```
%LOCALAPPDATA%\WPFLLM\wpfllm.db
```

The database is encrypted for privacy.

## Supported API Providers

Any OpenAI-compatible API:
- OpenAI
- Azure OpenAI
- Local LLMs (Ollama, LM Studio, etc.)
- Other compatible providers

Just change the API Endpoint in settings.

## License

MIT
