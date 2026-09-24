# Changelog

All notable changes to this project will be documented in this file.

## [1.0.1] - 2026-09-24
### Fixed
- Fixed critical null parameter exception in SQLite database session save.
- Fixed unhandled IO exceptions on protected files causing the DuplicateEngine to crash.
- Fixed cancellation token bug during hash calculations to throw properly.
- Resolved high-severity security vulnerability in `SixLabors.ImageSharp` by upgrading to 4.1.2.
- Updated Dashboard scan operation to run on a background thread using `IAsyncEnumerable` to prevent UI freezing.

## [1.0.0] - 2026-09-20
### Added
- Initial Release of Smart File Organizer.
