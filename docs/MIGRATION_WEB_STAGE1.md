# Migración a Web - Etapa 1 (servicios)

## Objetivo de la etapa

Mover lógica reutilizable desde la app desktop hacia una base web funcional mínima, sin romper la app de escritorio.

## Diagnóstico de arquitectura (estado inicial)

### Reutilizable directamente para web

- `AppPortable.Core`: contratos + modelos de dominio (sin dependencia de UI).
- `AppPortable.Infrastructure`:
  - `PdfExtractionService`
  - `ParagraphChunkingService`
  - `JsonPersistenceService`
  - `LocalStorageService`
  - `InfrastructureDocumentProcessor` (orquestación de pipeline)
  - `TesseractOcrService` (fallback OCR condicionado por disponibilidad)
- `AppPortable.Search`:
  - `SqliteIndexService`
  - `SqliteSearchService`

### Dependiente de desktop/WPF

- `AppPortable.Desktop/App.xaml.cs`
- `AppPortable.Desktop/MainWindow.xaml(.cs)`
- `AppPortable.Desktop/ViewModels/MainViewModel.cs`
- `AppPortable.Desktop/Commands/RelayCommand.cs`

Estas clases quedan intactas y siguen siendo la capa UI desktop.

### Adaptación mínima necesaria para web

- Composición DI y hosting (nuevo proyecto ASP.NET Core).
- Endpoints HTTP para:
  - carga de PDF y procesamiento
  - búsqueda
- UI mínima HTML/JS para probar flujo de punta a punta.

## Implementación de migración (etapa 1)

## Reutilizado (sin reescritura de lógica)

- Extracción PDF, chunking, persistencia JSON, indexación y búsqueda siguen en sus proyectos actuales.
- El proyecto web solo compone y expone por API los mismos servicios.

## Nuevos elementos web

- Nuevo proyecto `AppPortable.Web`.
- `Program.cs` con DI + endpoints `/api/documents`, `/api/search`, `/api/health`.
- `wwwroot/index.html` con flujo mínimo subir/procesar/buscar.
- DTOs de borde web en `Contracts/WebDtos.cs`.

## Dependencias desktop evitadas/desacopladas

- No se reutilizó ningún componente de WPF (`Application`, `Dispatcher`, `OpenFileDialog`, `MainWindow`).
- La web usa `IFormFile` y almacenamiento temporal para reemplazar selección de archivo de escritorio.
- La composición de servicios ya no depende del bootstrap en `App.xaml.cs`.

## Riesgos / pendientes (siguiente etapa)

1. **Chunking**: estrategia actual `one_page_one_chunk` es base mínima; mejorar relevancia después.
2. **OCR**: fallback existe pero no cubre OCR completo end-to-end para todas las casuísticas.
3. **Persistencia multiusuario**: almacenamiento local y SQLite actual no está aislado por usuario/sesión web.
4. **Seguridad**: faltan autenticación, autorización, validaciones avanzadas y límites de carga.
5. **Observabilidad**: faltan métricas, trazas estructuradas y manejo de errores más robusto.
6. **Tests web**: faltan pruebas de integración API/UI de la nueva capa web.
7. **Rendimiento**: no hay cola de trabajos ni procesamiento asíncrono desacoplado para PDFs grandes.

## Resultado de la etapa

- La lógica principal del servicio ya no está encerrada en desktop.
- Existe base web funcional mínima para subir, procesar e iniciar búsquedas.
- La app desktop se mantiene sin cambios funcionales en esta etapa.
