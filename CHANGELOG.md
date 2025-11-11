# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.9] - 2025-01-11

### Added - Core Documentation
- **Comprehensive README.md** with installation instructions, quick start guides, and complete usage examples
- **CHANGELOG.md** for tracking version history and changes
- **API_REFERENCE.md** - Complete API documentation for all classes and methods
- **BEST_PRACTICES.md** - Industry-standard patterns, common pitfalls, and best practices guide
- **CONTRIBUTING.md** - Contribution guidelines, code standards, and development workflow
- **.editorconfig** - Code style consistency configuration for the project

### Added - New Features & Utilities
- **SingletonMonoBehaviour<T>** - Thread-safe singleton base class with proper lifecycle management
- **GameUtilities** - Comprehensive utility class with:
  - Timing utilities (DelayedCall, Interpolate)
  - Transform utilities (DestroyChildren, Reset, GetChildren)
  - Collection utilities (GetRandom, Shuffle)
  - Math utilities (Remap, ClampAngle)
  - String utilities (FormatTime, FormatNumber)
  - Layer utilities (IsInLayer, SetLayerRecursively)
- **Easing Functions** - 15+ easing functions for smooth animations (Linear, Quad, Cubic, Sine, Expo, Back, Elastic, Bounce)

### Enhanced - EventManager
- Added thread safety with locking mechanism
- Improved error handling with try-catch in event dispatch
- Added comprehensive XML documentation
- New methods: `GetListenerCount()`, `HasListeners()`
- Better validation for null/empty event names
- Prevents duplicate listener registration
- Automatic cleanup of empty event entries

### Enhanced - UIManager
- Added null safety checks throughout
- Improved XML documentation for all public methods
- Better lifecycle management with virtual methods
- Enhanced error messages with component names
- Fixed iteration safety when hiding all UIs
- Standardized naming conventions (LAST_UI_REMEMBER_LIST)
- Protected virtual methods for derived class customization

### Enhanced - SaveManager
- Comprehensive error handling and validation
- Added null checks for all operations
- New methods: `UnregisterModule()`, `IsModuleRegistered()`, `GetRegisteredModuleCount()`
- Detailed logging for save/load operations
- Better integration with SaveSlotManager
- Improved XML documentation with usage examples

### Enhanced - NotificationManager
- Thread-safe queue implementation
- New methods: `GetQueuedNotificationCount()`, `ClearQueue()`, `IsDisplayingNotification()`
- Enhanced null checking for notification data
- Improved XML documentation
- Better lifecycle management

### Enhanced - FSM (Finite State Machine)
- Comprehensive XML documentation
- Improved null safety in all methods
- Better warning messages for debugging
- Thread-safe event manager initialization
- Protected virtual lifecycle methods

### Enhanced - UIBase
- Detailed XML documentation for abstract methods
- Better animation timing with configured durations
- Improved visibility state management
- Enhanced error messages
- Named parameter support for better code clarity

### Enhanced - SoundManager
- Added class-level XML documentation
- Already implements industry-standard patterns (pooling, spatial audio, mixing)

### Enhanced - Package Configuration
- Updated package.json with:
  - Expanded keywords for better discoverability
  - Repository information
  - License specification
  - Better description
- Fixed assembly definition namespaces (BB.Framework)
- Editor assembly now correctly references runtime assembly
- Editor assembly restricted to Editor platform

### Enhanced - Code Quality
- Improved dontDestroyOnLoad component with:
  - Duplicate prevention option
  - Unique tag-based identification
  - Better XML documentation
- Consistent private field naming with underscore prefix
- Better separation of concerns with regions
- Improved comment clarity throughout

### Changed
- Standardized singleton pattern across all managers
- Consistent naming conventions (Instance vs s_Instance → now documented)
- Improved error messages with component context
- Better code organization with regions
- Enhanced method parameter naming for clarity

### Fixed
- Null reference exceptions in manager initialization
- Memory leak prevention in event subscriptions
- Thread safety issues in singleton implementations
- Iteration safety when modifying collections during iteration
- Empty event entry cleanup in EventManager
- Assembly definition namespace inconsistencies

## [1.3.8] - Previous Release

### Added
- Notification UI updates
- Basic systems implementation

## Future Roadmap

### Planned Features
- Async/await support for loading operations
- Unity Events integration for inspector-based event binding
- Enhanced debugging tools and profiling
- More comprehensive unit tests
- Additional save system providers (cloud, binary)
- Addressables integration support
- UI animations library
- Scene management utilities

### Under Consideration
- Dependency injection container
- Localization system integration
- Analytics hooks
- Network synchronization support
