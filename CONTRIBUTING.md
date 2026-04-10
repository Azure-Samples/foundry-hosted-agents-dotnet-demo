# Contributing to This Learning Lab

This repository is a hands-on learning project for building hosted agents on Microsoft Foundry. We welcome contributions, bug reports, and feedback.

## How to Contribute

### Report a Bug or Request a Feature

Found an issue? Have an idea? Please [open a GitHub Issue](https://github.com/Azure-Samples/foundry-hosted-agents-dotnet-demo/issues/new).

Before submitting:
- Search existing issues to avoid duplicates
- Provide a clear description of the problem or feature request
- For bugs, include reproduction steps and expected vs. actual behavior
- For features, explain the learning value it adds to the lab

### Submit a Pull Request

1. **Fork the repository** and create a feature branch
2. **Make your changes** with clear commit messages
3. **Test locally** — run `dotnet build && dotnet test`
4. **Update docs** if your change affects how a scenario works
5. **Submit your PR** with a description of what you changed and why

### Code Style

- Follow [C# coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Keep code comments minimal — prefer self-documenting code
- Scenarios should follow the established template: `Program.cs` + `ChatClientAgent` + hosting adapter

## Code of Conduct

This project follows the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). Please report concerns to [opencode@microsoft.com](mailto:opencode@microsoft.com).

## Questions?

- Check the [main README](README.md) for core concepts
- See [docs/SCENARIOS.md](docs/SCENARIOS.md) for the full scenario catalog
- Review [Microsoft Learn documentation](https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents) for official hosted agents concepts
