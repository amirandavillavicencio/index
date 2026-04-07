# Gabriela (AppPortable)

Gabriela es una aplicación de escritorio **WPF (.NET 8)** para procesar PDFs de forma local (offline-first), ejecutar OCR con Tesseract, generar chunks, persistir en JSON e indexar en **SQLite FTS5**.

## Objetivo actual

Este repositorio está preparado para generar una **carpeta portable para Windows x64** que se pueda copiar a otra PC y ejecutar con doble clic, **sin instalador** y **sin requerir permisos de administrador**.

## Qué se reutiliza (sin reescribir lógica de negocio)

Se mantiene la lógica existente de:

1. Carga de PDF.
2. Extracción de texto del PDF.
3. OCR con Tesseract (fallback).
4. Generación de chunks.
5. Persistencia local en JSON.
6. Indexación local en SQLite.
7. Búsqueda local y visualización de resultados.

Proyectos de dominio y servicios mantenidos:

- `AppPortable.Core`
- `AppPortable.Infrastructure`
- `AppPortable.Search`

## Requisitos

- Windows 10/11 x64.
- .NET SDK 8.0+ solo para compilar/publicar (el usuario final **no** lo necesita).
- Tesseract portable (carpeta local con `tesseract.exe` y `tessdata`) para OCR dentro del paquete portable.

## Publicación portable (comando esperado)

```bash
dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false -o .\publish-portable
```

También puedes usar el script:

```powershell
pwsh ./scripts/publish-portable.ps1
```

## Estructura esperada de la carpeta portable

```text
publish-portable/
├─ Gabriela.exe
├─ AppPortable.Core.dll
├─ AppPortable.Infrastructure.dll
├─ AppPortable.Search.dll
├─ ...runtime self-contained de .NET...
├─ tesseract/
│  ├─ tesseract.exe
│  └─ tessdata/
│     ├─ spa.traineddata
│     └─ eng.traineddata
└─ data/
   ├─ documents/
   ├─ json/
   ├─ chunks/
   ├─ index/
   │  └─ appportable.db
   ├─ temp/
   └─ logs/
```

> Nota: `data/` se crea automáticamente junto al ejecutable cuando hay permisos de escritura. Si la carpeta no es escribible, la app usa `%LOCALAPPDATA%\Gabriela`.

## Ejecución de usuario final

1. Copiar la carpeta `publish-portable` a la PC destino.
2. Verificar que exista `tesseract\tesseract.exe` y `tesseract\tessdata`.
3. Ejecutar `Gabriela.exe` con doble clic.
4. Flujo en UI: **Cargar PDF → Procesar/OCR → Buscar → Ver resultados**.

## Build y tests

```bash
dotnet restore AppPortable.Desktop/AppPortable.Desktop.csproj
dotnet restore AppPortable.Tests/AppPortable.Tests.csproj
dotnet build AppPortable.Desktop/AppPortable.Desktop.csproj -c Release --no-restore
dotnet build AppPortable.Tests/AppPortable.Tests.csproj -c Release --no-restore
dotnet test AppPortable.Tests/AppPortable.Tests.csproj -c Release --no-build
```

## Dependencias que causaban fricción y cómo se resolvieron

- **Fricción:** Dependencia implícita de Tesseract instalado en `PATH`/`Program Files`.
  - **Cambio:** Se prioriza carpeta relativa portable (`./tesseract` o `./tools/tesseract`) al iniciar la app.
- **Fricción:** Ruta de datos fija orientada a `%LOCALAPPDATA%\AppPortable`.
  - **Cambio:** Se habilita modo portable escribiendo en `./data` junto al ejecutable, con fallback seguro a `%LOCALAPPDATA%\Gabriela`.
- **Fricción:** Publicación documentada como single-file.
  - **Cambio:** Publicación portable multiarchivo self-contained (sin requerir instalación de .NET).

## Limitaciones pendientes

- OCR depende de que la carpeta portable incluya binarios y `tessdata` válidos de Tesseract.
- Esta app sigue siendo de escritorio Windows (WPF); no hay soporte de ejecución UI fuera de Windows.

## Estructura del repositorio

```text
.
├─ AppPortable.Core/
├─ AppPortable.Infrastructure/
├─ AppPortable.Search/
├─ AppPortable.Desktop/
├─ AppPortable.Tests/
├─ docs/
└─ scripts/
```
