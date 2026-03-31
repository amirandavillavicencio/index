# AppPortable

Aplicación de escritorio Windows para procesamiento documental local:

PDF → extracción de texto → documento estructurado → persistencia JSON → chunking → indexación SQLite FTS5 → búsqueda local.

## Requisitos

- Windows 10/11 x64
- .NET SDK 8.0+

## Instalación

```bash
git clone <repo>
cd index
dotnet restore AppPortable.sln
```

## Uso rápido

```bash
dotnet run --project AppPortable.Desktop/AppPortable.Desktop.csproj
```

1. Click en **Cargar PDF**.
2. Se procesa, serializa e indexa localmente.
3. Buscar términos en la caja central.
4. Ver detalles del resultado en el panel derecho.

## Estructura

- `AppPortable.Core`: modelos, contratos y pipeline.
- `AppPortable.Infrastructure`: extracción PDF iText7, storage local, JSON, chunking.
- `AppPortable.Search`: SQLite + FTS5 para indexación y búsqueda.
- `AppPortable.Desktop`: UI WPF MVVM y comandos.
- `AppPortable.Tests`: tests de extractor, chunking, persistencia, índice y pipeline.
- `docs`: arquitectura y build.

## Build / Publish

```bash
dotnet build AppPortable.sln -c Release
dotnet test AppPortable.Tests/AppPortable.Tests.csproj -c Release

dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj \
  -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true
```
