# Architecture (Phase 1 Baseline)

## Layers

- **AppPortable.Core**
  - Minimal shared models and contracts.
  - No external infrastructure dependencies.

- **AppPortable.Infrastructure**
  - Placeholder layer for file system, persistence, and adapters.
  - References `AppPortable.Core`.

- **AppPortable.Search**
  - Placeholder layer for search/index abstractions and implementations.
  - References `AppPortable.Core`.

- **AppPortable.Desktop**
  - WPF application entrypoint and shell UI.
  - References `Core`, `Infrastructure`, and `Search`.

- **AppPortable.Tests**
  - Automated tests targeting solution structure and baseline behavior.
  - References `Core`, `Infrastructure`, and `Search`.

## Dependency direction

`Desktop -> (Core, Infrastructure, Search)`

`Infrastructure -> Core`

`Search -> Core`

`Tests -> (Core, Infrastructure, Search)`

## Notes

Phase 1 intentionally avoids implementing real document ingestion,
indexing, OCR, and full MVVM behavior.
