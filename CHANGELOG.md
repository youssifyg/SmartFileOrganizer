# Changelog

## [1.0.1] - 2026-09-25

### Added
- Implemented a custom WPF splash screen to mask background Dependency Injection and database initialization during startup.
- Introduced a comprehensive `AppDomain` crash logger to gracefully catch and write unhandled exceptions to `%LOCALAPPDATA%`.
- Native SQLite library extraction (`IncludeNativeLibrariesForSelfExtract`) enabled for seamless single-file portable deployments.

### Fixed
- Fixed critical application startup deadlocks caused by synchronous Dispatcher conflicts.
- Offloaded duplicate scan processing to background threads utilizing `IAsyncEnumerable`, preventing UI freezing during heavy IO operations.
- Fixed `DllNotFoundException` for SQLite occurring in self-contained publishing scenarios.
- Prevented crashes when encountering locked or protected files within the Duplicate Engine.
- Resolved Null Reference Exceptions inside SQLite parameters that caused unexpected scan aborts.
- Fixed UI localization state issues, ensuring language change events correctly propagate across ViewModels.

### Security
- Migrated the visual similarity image processing engine to **SkiaSharp** to avoid strict commercial license blocks and address high-severity vulnerabilities associated with older imaging libraries.

---
