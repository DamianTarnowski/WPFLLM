# Contributing to WPFLLM

First off, thanks for taking the time to contribute! 🎉

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
- [Code Style](#code-style)
- [Testing](#testing)
- [Commit Messages](#commit-messages)
- [Pull Request Process](#pull-request-process)

## Code of Conduct

This project follows a standard code of conduct. Please be respectful and constructive in all interactions.

## How Can I Contribute?

### 🐛 Reporting Bugs

Before creating bug reports, please check existing issues. When creating a bug report, use the [bug report template](.github/ISSUE_TEMPLATE/bug_report.md) and include:

- **Clear title** describing the issue
- **Steps to reproduce** the behavior
- **Expected vs actual behavior**
- **Screenshots** if applicable
- **Environment** (OS version, .NET version, WPFLLM version)

### ✨ Suggesting Features

Feature suggestions are welcome! Use the [feature request template](.github/ISSUE_TEMPLATE/feature_request.md) and include:

- **Use case** - Why is this feature needed?
- **Proposed solution** - How should it work?
- **Alternatives considered** - Other approaches you've thought about

### 🔧 Pull Requests

1. Fork the repo and create your branch from `master`
2. Follow the existing code style
3. Add tests for new functionality
4. Update documentation if needed
5. Ensure all tests pass (`dotnet test`)
6. Submit a pull request using our [PR template](.github/PULL_REQUEST_TEMPLATE.md)

## Development Setup

### Prerequisites

- Windows 10/11 (64-bit)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Rust](https://rustup.rs/) (only if modifying the tokenizer)

### Getting Started

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/WPFLLM.git
cd WPFLLM

# Create feature branch
git checkout -b feature/my-feature

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run
```

### Project Structure

```
WPFLLM/
├── Converters/          # WPF value converters
├── Models/              # Data models and DTOs
├── Services/            # Business logic services
├── ViewModels/          # MVVM ViewModels
├── Views/               # XAML views
├── Themes/              # UI themes and styles
├── docs/                # Documentation
├── TokenizerRust/       # Rust tokenizer (FFI)
└── WPFLLM.Tests/        # Test project
    ├── Integration/     # Integration tests
    ├── Unit/            # Unit tests
    └── RealApi/         # Real API tests
```

## Code Style

### C# Guidelines

- Use C# 12 features where appropriate
- Follow [Microsoft naming conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `async/await` for async operations
- Prefer LINQ over manual loops
- Keep methods focused and small (< 30 lines)
- Use dependency injection for services

### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `ChatService` |
| Interfaces | IPascalCase | `IChatService` |
| Methods | PascalCase | `SendMessageAsync` |
| Properties | PascalCase | `IsLoading` |
| Private fields | _camelCase | `_chatService` |
| Parameters | camelCase | `messageContent` |
| Constants | PascalCase | `MaxRetryCount` |

### XAML Guidelines

- Use meaningful `x:Name` attributes
- Prefer styles over inline properties
- Use resource dictionaries for reusable styles
- Follow MVVM pattern (no code-behind logic)

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific category
dotnet test --filter "TestCategory=Unit"
dotnet test --filter "TestCategory=Integration"
dotnet test --filter "TestCategory=RealApi"  # Requires API key

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Writing Tests

- Use MSTest with FluentAssertions
- Use NSubstitute for mocking
- Follow Arrange-Act-Assert pattern
- Name tests: `MethodName_Scenario_ExpectedResult`

```csharp
[TestMethod]
public async Task SendMessage_ValidInput_ReturnsResponse()
{
    // Arrange
    var service = CreateService();
    var message = "Hello";

    // Act
    var result = await service.SendMessageAsync(message);

    // Assert
    result.Should().NotBeNullOrEmpty();
}
```

### Real API Tests

To run tests against real APIs, set up your API key:

```bash
# PowerShell
$env:OPENROUTER_API_KEY = "your-key-here"
dotnet test --filter "TestCategory=RealApi"
```

## Commit Messages

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types

| Type | Description |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `style` | Formatting, no code change |
| `refactor` | Code restructuring |
| `perf` | Performance improvement |
| `test` | Adding/updating tests |
| `chore` | Maintenance tasks |

### Examples

```bash
feat(chat): add message editing capability
fix(rag): handle empty document chunks
docs: update installation instructions
test(encryption): add roundtrip tests
refactor(services): extract common HTTP logic
```

## Pull Request Process

1. **Update your branch** with the latest `master`
2. **Run all tests** locally: `dotnet test`
3. **Update documentation** if needed
4. **Fill out the PR template** completely
5. **Request review** from maintainers
6. **Address feedback** promptly
7. **Squash commits** if requested

### PR Checklist

- [ ] Tests pass locally
- [ ] Code follows style guidelines
- [ ] Documentation updated
- [ ] No new warnings
- [ ] Meaningful commit messages
- [ ] PR template completed

## Questions?

Feel free to [open an issue](https://github.com/DamianTarnowski/WPFLLM/issues/new) with your question!

---

Thank you for contributing to WPFLLM! 🚀
