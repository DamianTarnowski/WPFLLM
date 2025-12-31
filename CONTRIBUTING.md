# Contributing to WPFLLM

First off, thanks for taking the time to contribute! 🎉

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues. When creating a bug report, include:

- **Clear title** describing the issue
- **Steps to reproduce** the behavior
- **Expected behavior** vs actual behavior
- **Screenshots** if applicable
- **Environment** (OS version, .NET version)

### Suggesting Features

Feature suggestions are welcome! Please include:

- **Use case** - Why is this feature needed?
- **Proposed solution** - How should it work?
- **Alternatives considered** - Other approaches you've thought about

### Pull Requests

1. Fork the repo and create your branch from `master`
2. Follow the existing code style
3. Add tests if applicable
4. Update documentation if needed
5. Ensure the build passes

## Code Style

- Use C# 12 features where appropriate
- Follow Microsoft naming conventions
- Use `async/await` for async operations
- Prefer LINQ over manual loops
- Keep methods focused and small

## Commit Messages

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat(scope): add new feature
fix(scope): fix bug
docs: update documentation
chore: maintenance tasks
refactor: code refactoring
test: add/update tests
```

Examples:
```
feat(chat): add message editing
fix(rag): handle empty documents
docs: update README installation
```

## Development Setup

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/WPFLLM.git
cd WPFLLM

# Create feature branch
git checkout -b feature/my-feature

# Make changes and test
dotnet build
dotnet run

# Commit and push
git add .
git commit -m "feat: add my feature"
git push origin feature/my-feature
```

## Questions?

Feel free to open an issue with your question!
