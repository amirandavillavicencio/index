# AppPortable

AppPortable es una aplicación de escritorio **WPF (.NET 8)** para procesar PDFs de forma local (offline-first), generar metadatos estructurados, persistirlos en JSON e indexar contenido en **SQLite FTS5** para búsqueda rápida.

## Overview

Flujo principal actual:

1. Selección de PDF desde la UI.
2. Copia del archivo al almacenamiento local de la app.
3. Extracción de texto por página (capa nativa PDF).
4. Fallback OCR (solo si está disponible y si hay páginas sin texto).
5. Construcción de documento procesado + chunks.
6. Persistencia local en JSON.
7. Indexación en SQLite FTS5.
8. Búsqueda local con ranking y snippet.

## Stack real

- **Runtime:** .NET 8
- **UI:** WPF (Windows)
- **Extracción PDF:** iText7
- **Persistencia:** JSON local (`snake_case`)
- **Índice/Búsqueda:** SQLite + FTS5
- **Testing:** xUnit (.NET test project)
- **CI/CD:** GitHub Actions (`windows-latest`)

## Arquitectura resumida

- `AppPortable.Core`: contratos, modelos y orquestación de pipeline.
- `AppPortable.Infrastructure`: servicios de extracción PDF, OCR fallback, storage local y persistencia JSON.
- `AppPortable.Search`: indexación y consulta sobre SQLite FTS5.
- `AppPortable.Desktop`: aplicación WPF/MVVM.
- `AppPortable.Web`: base web mínima (ASP.NET Core) reutilizando servicios existentes.
- `AppPortable.Tests`: pruebas de pipeline, extracción, chunking, persistencia e índice.

Más detalle: `docs/ARCHITECTURE.md`.

## Requisitos

- **OS para ejecución UI:** Windows 10/11 x64.
- **SDK:** .NET 8.0+.
- **Opcional (OCR):** binario `tesseract` disponible en `PATH`.

> Nota: el pipeline funciona sin OCR. Si no hay OCR disponible, las páginas sin texto nativo quedan marcadas como no extraídas por OCR.

## Restore / Build / Test / Publish

```bash
dotnet restore AppPortable.sln
dotnet build AppPortable.sln -c Release --no-restore
dotnet test AppPortable.Tests/AppPortable.Tests.csproj -c Release --no-build
dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Salida de publicación esperada:

- Carpeta: `AppPortable.Desktop/bin/Release/net8.0-windows/win-x64/publish/`
- Ejecutable self-contained single-file para Windows x64.

## Uso rápido

```bash
dotnet run --project AppPortable.Desktop/AppPortable.Desktop.csproj
```

Uso en UI:

1. Click en **Cargar PDF**.
2. Esperar procesamiento/indexación local.
3. Escribir consulta en caja de búsqueda.
4. Revisar resultados y detalle.

## Uso rápido (web mínima)

```bash
dotnet run --project AppPortable.Web/AppPortable.Web.csproj
```

Luego abrir la raíz de la app web para probar el flujo:

1. Subir PDF.
2. Procesar/indexar.
3. Buscar.

## Estructura de carpetas

```text
.
├─ AppPortable.Core/
├─ AppPortable.Infrastructure/
├─ AppPortable.Search/
├─ AppPortable.Desktop/
├─ AppPortable.Web/
├─ AppPortable.Tests/
├─ .github/workflows/build.yml
└─ docs/
   ├─ ARCHITECTURE.md
   └─ BUILD.md
```

## OCR local y tessdata

Estado actual del OCR:

- El proyecto detecta disponibilidad de `tesseract` en `PATH`.
- Existe servicio OCR de fallback (`TesseractOcrService`), pero en el estado actual **no ejecuta render/OCR real por página**; agrega advertencia cuando aplica fallback.
- Si tu instalación de Tesseract requiere `tessdata` en ubicación específica, configúrala según tu instalación local (por ejemplo, `TESSDATA_PREFIX`).

## Limitaciones actuales

- OCR no está implementado end-to-end (fallback preparado, pero sin extracción OCR real de imagen por página).
- La UI está orientada a Windows (WPF).
- Persistencia e índice son locales (sin sincronización remota).
