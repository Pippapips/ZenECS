# Contributing

> Docs / Community / Contributing

Thank you for your interest in contributing to ZenECS! This guide will help you get started.

## How to Contribute

### Reporting Issues

Found a bug or have a feature request?

1. **Check existing issues** - Search for similar issues first
2. **Create new issue** - Use appropriate template
3. **Provide details** - Include steps to reproduce, environment info
4. **Be patient** - Maintainers will respond when available

### Contributing Code

Want to submit code changes?

1. **Fork the repository**
2. **Create a branch** - `feature/my-feature` or `fix/my-bug`
3. **Make changes** - Follow coding standards
4. **Write tests** - Ensure your code is tested
5. **Submit PR** - Include description and reference issues

### Contributing Documentation

Documentation improvements are always welcome!

1. **Find documentation issues** - Check TODO items
2. **Improve clarity** - Fix typos, add examples
3. **Add missing docs** - Document new features
4. **Submit PR** - Follow documentation guidelines

## Development Setup

### Prerequisites

- **.NET SDK 8.0+** or **Unity 2021.3+**
- **Git** for version control
- **IDE** (Visual Studio, Rider, or VS Code)

### Getting Started

1. **Clone repository**
   ```bash
   git clone https://github.com/Pippapips/ZenECS.git
   cd ZenECS
   ```

2. **Build Core**
   ```bash
   cd src
   dotnet build ZenECS.Core.csproj
   ```

3. **Build Adapter Unity**
   ```bash
   dotnet build ZenECS.Adapter.Unity.csproj
   ```

4. **Run tests** (if available)
   ```bash
   dotnet test
   ```

## Coding Standards

### C# Style

- **C# 10+ features** - Use modern C# syntax
- **Nullable reference types** - Enable nullable context
- **XML documentation** - Document all public APIs
- **Naming conventions** - Follow C# conventions

### Code Organization

- **One class per file** - Keep files focused
- **Namespace organization** - Logical grouping
- **Partial classes** - For large classes (like World)
- **Internal types** - Use for implementation details

### Documentation

- **XML comments** - All public APIs
- **Examples** - Include code examples
- **Remarks** - Explain behavior and edge cases
- **See also** - Link to related APIs

## Pull Request Process

### Before Submitting

- [ ] Code compiles without errors
- [ ] Tests pass (if applicable)
- [ ] Documentation updated
- [ ] Changelog updated (if needed)
- [ ] Code follows style guidelines

### PR Description

Include:

- **What** - What changes were made
- **Why** - Why these changes are needed
- **How** - How the changes work
- **Testing** - How to test the changes

### Review Process

1. **Automated checks** - CI/CD runs tests
2. **Code review** - Maintainers review code
3. **Feedback** - Address review comments
4. **Merge** - Once approved, PR is merged

## Areas for Contribution

### High Priority

- **Documentation** - Improve existing docs, add examples
- **Samples** - Create example projects
- **Tests** - Add unit tests and integration tests
- **Performance** - Optimize hot paths

### Medium Priority

- **Features** - Implement requested features
- **Bug fixes** - Fix reported issues
- **Tooling** - Improve editor tools
- **Localization** - Translate documentation

### Low Priority

- **Code cleanup** - Refactor and improve code
- **Examples** - Add more code examples
- **Tutorials** - Create tutorial content

## Code of Conduct

Please read and follow our [Code of Conduct](./code-of-conduct.md).

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Questions?

- **GitHub Issues** - For bug reports and feature requests
- **Discussions** - For questions and discussions
- **Email** - For private inquiries

## See Also

- [Code of Conduct](./code-of-conduct.md) - Community guidelines
- [Documentation Guidelines](../references/documentation-guidelines.md) - Writing docs
- [Architecture](../overview/architecture.md) - Understanding the codebase
