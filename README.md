# AppPortable

Base reconstruction for a layered .NET 8 solution.

## Solution structure

- `AppPortable.Core`: shared domain contracts and entities.
- `AppPortable.Infrastructure`: infrastructure layer skeleton.
- `AppPortable.Search`: search layer skeleton.
- `AppPortable.Desktop`: WPF desktop shell for Windows x64.
- `AppPortable.Tests`: automated test project.
- `docs/`: architecture and build documentation.

## Current phase scope

This repository currently provides a clean, compilable baseline only.
Advanced document processing (PDF extraction, chunking, OCR, and indexing pipeline)
will be implemented in later phases.
