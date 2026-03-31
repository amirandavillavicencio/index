# Architecture

## Capas

## AppPortable.Core
- Entidades de dominio (`ProcessedDocument`, `DocumentPage`, `DocumentChunk`, `SearchResult`, `ExtractionSummary`).
- Enum `ExtractionLayer`.
- Interfaces (`IPdfExtractionService`, `IJsonPersistenceService`, `ILocalStorageService`, `IChunkingService`, `IIndexService`, `ISearchService`, `IDocumentProcessor`, `IOcrService`).
- Pipeline de orquestación `DocumentProcessor`.

## AppPortable.Infrastructure
- `PdfExtractionService`: extracción por página con iText7.
- `LocalStorageService`: manejo de `%LOCALAPPDATA%\AppPortable`.
- `JsonPersistenceService`: serialización `snake_case`.
- `ParagraphChunkingService`: chunking por párrafos con overlap del 10%.

## AppPortable.Search
- `SqliteIndexService`: tablas base + FTS5 + indexación y rebuild.
- `SqliteSearchService`: búsqueda `MATCH`, ranking `bm25`, `snippet`.

## AppPortable.Desktop
- WPF + MVVM con `MainViewModel`.
- Comandos: carga, búsqueda, reindexación.
- Paneles: documentos, búsqueda/resultados, detalle.

## Flujo del pipeline

1. Copia PDF al storage local.
2. Extrae texto por página.
3. Construye `ProcessedDocument`.
4. Calcula `document_id` estable por hash.
5. Genera chunks.
6. Persiste JSON de documento y chunks.
7. Indexa chunks en SQLite FTS5.
8. Devuelve resultado para UI.

## Decisiones técnicas

- OCR solo preparado a nivel de interfaz (`IOcrService`) para mantener estabilidad inicial.
- SQLite FTS5 local para cero dependencias externas.
- JSON local legible e interoperable.
