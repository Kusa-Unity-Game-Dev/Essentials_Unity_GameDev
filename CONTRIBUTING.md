# Contributing to Game Essentials

Thank you for considering contributing to Game Essentials! This document provides guidelines for contributing to the project.

## Code of Conduct

- Be respectful and constructive in all interactions
- Focus on what is best for the community
- Show empathy towards other community members

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue with:

1. **Clear title** - Brief description of the issue
2. **Description** - Detailed explanation of the problem
3. **Steps to reproduce** - Step-by-step instructions
4. **Expected behavior** - What should happen
5. **Actual behavior** - What actually happens
6. **Environment** - Unity version, package version, OS
7. **Screenshots/Logs** - If applicable

### Suggesting Features

Feature requests are welcome! Please include:

1. **Use case** - Why is this feature needed?
2. **Proposed solution** - How should it work?
3. **Alternatives** - Any alternative approaches considered
4. **Examples** - Code examples or mockups if possible

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Follow the code style** defined in `.editorconfig`
3. **Write clear commit messages** following conventional commits
4. **Add tests** if adding new functionality
5. **Update documentation** for any API changes
6. **Ensure all tests pass** before submitting
7. **Submit a pull request** with a clear description

## Development Setup

1. Clone the repository
2. Open in Unity 6000.0 or higher
3. The package can be tested by adding it to a test project

## Code Style Guidelines

### Naming Conventions

- **Classes**: PascalCase (e.g., `UIManager`, `SaveManager`)
- **Methods**: PascalCase (e.g., `ShowUI`, `SaveModule`)
- **Private fields**: _camelCase with underscore prefix (e.g., `_eventManager`)
- **Public fields**: PascalCase (e.g., `IsVisible`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `MAX_POOL_SIZE`)
- **Parameters**: camelCase (e.g., `eventName`, `listener`)

### Code Structure

```csharp
using System;
using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Class documentation
    /// </summary>
    public class ExampleClass : MonoBehaviour
    {
        // Serialized fields
        [SerializeField] private int _exampleField;
        
        // Private fields
        private int _privateField;
        
        // Public properties
        public int PublicProperty { get; private set; }
        
        // Unity lifecycle methods
        private void Awake() { }
        private void Start() { }
        private void OnDestroy() { }
        
        // Public methods
        public void PublicMethod() { }
        
        // Private methods
        private void PrivateMethod() { }
    }
}
```

### Comments and Documentation

- Add XML documentation comments for all public APIs
- Use `//` for inline comments, sparingly
- Keep comments concise and meaningful
- Update comments when code changes

Example:
```csharp
/// <summary>
/// Saves a specific module to the specified save slot.
/// </summary>
/// <param name="slotName">The name of the save slot. Cannot be null or empty.</param>
/// <param name="moduleType">The type of module to save</param>
public void SaveModule(string slotName, ESaveModule moduleType)
{
    // Implementation
}
```

## Testing Guidelines

### Unit Tests

- Write tests for new functionality
- Keep tests simple and focused
- Use descriptive test names
- Follow AAA pattern (Arrange, Act, Assert)

Example:
```csharp
[Test]
public void EventManager_AddListener_RegistersListener()
{
    // Arrange
    var eventManager = new EventManager<string>();
    bool listenerCalled = false;
    
    // Act
    eventManager.AddListener("Test", (data) => listenerCalled = true);
    eventManager.DispatchEvent("Test", "data");
    
    // Assert
    Assert.IsTrue(listenerCalled);
}
```

### Manual Testing

- Test in Unity Editor
- Test in builds (when applicable)
- Test on target platforms
- Test edge cases and error conditions

## Commit Message Guidelines

Follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation only
- `style:` - Code style changes (formatting, etc.)
- `refactor:` - Code refactoring
- `perf:` - Performance improvements
- `test:` - Adding or updating tests
- `chore:` - Maintenance tasks

Examples:
```
feat: add async save support to SaveManager
fix: prevent memory leak in EventManager
docs: update API reference for SoundManager
refactor: improve UIManager null safety
```

## Documentation Guidelines

### README Updates

- Keep the quick start section simple
- Add new features to the features list
- Update examples when APIs change

### API Reference

- Document all public methods and properties
- Include parameter descriptions
- Provide code examples
- Note any exceptions that may be thrown

### Best Practices Guide

- Add patterns you've found useful
- Include anti-patterns to avoid
- Provide real-world examples

## Release Process

Releases are managed by maintainers:

1. Update version in `package.json`
2. Update `CHANGELOG.md` with changes
3. Create a git tag
4. Publish release notes

## Questions?

- Open an issue for general questions
- Tag maintainers for urgent matters
- Check existing issues and documentation first

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Recognition

Contributors will be recognized in:
- Release notes
- CHANGELOG.md
- Project documentation

Thank you for contributing to Game Essentials! 🎮
