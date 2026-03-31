# Architecture

## Visión general

AppPortable implementa un pipeline local para procesamiento documental:

**PDF -> extracción por página -> (fallback OCR) -> documento procesado -> chunking -> JSON -> SQLite FTS5 -> búsqueda local**

Todo el flujo se ejecuta localmente, sin servicios externos obligatorios.

## Capas y responsabilidades

## 1) AppPortable.Core

Responsabilidad: contratos y orquestación de dominio.

Incluye:

- Modelos de dominio (`ProcessedDocument`, `DocumentPage`, `DocumentChunk`, `SearchResult`, `ExtractionSummary`).
- Enum de capa de extracción (`ExtractionLayer`).
- Interfaces de servicios (`IPdfExtractionService`, `IOcrService`, `IChunkingService`, `IJsonPersistenceService`, `ILocalStorageService`, `IIndexService`, `ISearchService`, `IDocumentProcessor`).
- `DocumentProcessor` como orquestador del pipeline.

Core no depende de UI ni de detalles de infraestructura concretos.

## 2) AppPortable.Infrastructure

Responsabilidad: implementación de servicios de entrada/salida y procesamiento técnico.

Incluye:

- `PdfExtractionService` (iText7) para texto nativo por página.
- `TesseractOcrService` como fallback OCR condicionado por disponibilidad de `tesseract`.
- `LocalStorageService` para estructura local de archivos.
- `JsonPersistenceService` para serialización/deserialización JSON en `snake_case`.
- `ParagraphChunkingService` para segmentación por párrafos con overlap.
- `InfrastructureDocumentProcessor` como composición de dependencias de infraestructura sobre `DocumentProcessor`.

### OCR fallback (estado actual)

- El fallback OCR se intenta solo si está habilitado y hay páginas sin texto.
- La disponibilidad OCR depende de detectar `tesseract` en `PATH`.
- En el estado actual, el servicio no realiza OCR completo por imagen/página; conserva texto existente y emite advertencias de no aplicación real.

## 3) AppPortable.Search

Responsabilidad: indexación y búsqueda full-text local.

Incluye:

- `SqliteIndexService`: inicialización de esquema, indexación y rebuild.
- `SqliteSearchService`: consultas `MATCH` sobre FTS5, ranking (`bm25`) y snippets.

### SQLite FTS5

El índice FTS5 permite búsquedas textuales rápidas y totalmente locales sobre chunks de documento.

## 4) AppPortable.Desktop

Responsabilidad: presentación y flujo de usuario en escritorio Windows.

Incluye:

- Aplicación WPF + MVVM.
- `MainViewModel` con comandos para cargar PDF, buscar y reindexar.
- Integración del pipeline de procesamiento con almacenamiento e índice locales.

## 5) AppPortable.Tests

Responsabilidad: validación automatizada del comportamiento actual.

Cobertura principal:

- extracción PDF,
- chunking,
- persistencia JSON,
- indexación/búsqueda,
- pipeline extremo a extremo.

## Flujo detallado del pipeline

1. `LocalStorageService` asegura estructura local y copia el PDF fuente.
2. `PdfExtractionService` extrae texto por página.
3. `DocumentProcessor` decide si activar fallback OCR.
4. Se calcula `document_id` estable.
5. `ParagraphChunkingService` genera chunks.
6. `JsonPersistenceService` guarda documento y chunks en JSON.
7. `SqliteIndexService` indexa chunks en FTS5.
8. UI consulta mediante `SqliteSearchService` y muestra resultados.

## Persistencia JSON

Persistencia local para:

- documento procesado,
- chunks derivados.

Beneficios:

- trazabilidad,
- inspección sencilla,
- reindexación sin reprocesar PDF.

## Pipeline de CI/CD (GitHub Actions)

Workflow único: `.github/workflows/build.yml`.

Pasos:

1. `dotnet restore AppPortable.sln`
2. `dotnet build AppPortable.sln -c Release --no-restore`
3. `dotnet test AppPortable.Tests/AppPortable.Tests.csproj -c Release --no-build`
4. `dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true`
5. upload de artifact de publicación para `win-x64`.
