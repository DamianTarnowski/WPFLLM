# Changelog

All notable changes to WPFLLM will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive test suite with **317 tests** (unit, integration, real API)
- Real API integration tests for OpenRouter
- `.env` file support for API keys in tests
- Detailed testing documentation in README

## [1.0.0] - 2026-01-01

### Added

#### Core Features
- **Intelligent Chat** with streaming responses
- Multiple conversation management with history
- Markdown rendering support
- RAG Debug Panel with sources, scores, and latency metrics

#### RAG System
- **Hybrid Retrieval**: Vector search + FTS5 keyword search with RRF fusion
- Document import (.txt, .md, .json, .csv, .pdf, .docx)
- Configurable chunk size and overlap
- Debug panel showing topK, similarity scores, retrieval mode

#### Document Analysis
- Summary generation
- Intent detection with confidence scores
- Red flag identification with severity levels
- Compliance checklist verification
- Suggested response drafting

#### Local Embedding Models
- ONNX-based local inference
- Support for multilingual E5 models (Small/Base/Large/Instruct)
- **Rust HuggingFace tokenizer** for correct special token handling
- Mean pooling + L2 normalization
- 20x improvement in embedding discrimination (GAP: 0.7% → 14.5%)

#### Security
- AES-256-GCM encryption for data at rest
- DPAPI key protection tied to Windows user
- Offline mode indicator
- Network call counter for transparency
- Zero telemetry, zero data leakage

#### UI/UX
- Modern dark theme with accent colors
- Multi-language support (English, Polski)
- Status bar with offline/encryption/network indicators
- Professional RAG debug metrics display

#### Infrastructure
- SQLite database with FTS5 full-text search
- Channel<T>-based ingestion pipeline with backpressure
- Resumable model downloads from HuggingFace
- Comprehensive error handling and retry logic

### Technical Stack
- .NET 10.0
- WPF with CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- SQLite + Dapper
- ONNX Runtime
- Rust FFI for tokenization

---

## Version History

| Version | Date | Highlights |
|---------|------|------------|
| 1.0.0 | 2026-01-01 | Initial release with full RAG, encryption, local embeddings |

---

## Upgrade Notes

### Upgrading to 1.0.0

This is the initial release. No upgrade steps required.

### Database Migrations

The application handles database migrations automatically on startup. Your data is preserved during upgrades.

### Breaking Changes

None in this release.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for how to contribute to this project.
