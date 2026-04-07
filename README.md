# Gabriela (AppPortable)

Gabriela es una aplicación de escritorio **WPF (.NET 8)** para procesar PDFs offline, aplicar OCR con Tesseract como fallback, generar chunks, persistir JSON e indexar en **SQLite FTS5**.

## Objetivo portable

El resultado principal de publicación es una carpeta **portable** en:

- `artifact/publish-portable/`

Esa carpeta se puede copiar a otra PC Windows 10/11 x64 y ejecutar con doble clic, sin instalador y sin requerir .NET instalado globalmente.

## Publicación portable (comando directo)

```bash
dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false /p:PublishTrimmed=false -o ./artifact/publish-portable
```

## Publicación portable (script recomendado)

```powershell
pwsh ./scripts/publish-portable.ps1
```

El script limpia y regenera `artifact/publish-portable`.

## Estructura esperada

```text
artifact/publish-portable/
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

## Resolución de rutas en modo portable

La app usa rutas relativas al ejecutable (`AppContext.BaseDirectory`) con esta prioridad:

1. OCR: `./tesseract`
2. OCR alternativo: `./tools/tesseract`
3. Datos locales: `./data`

Si `./data` no es escribible, usa fallback seguro a `%LOCALAPPDATA%\Gabriela`.

> No se depende de `PATH`, `Program Files` ni variables de entorno manuales para encontrar Tesseract en modo portable.

## Preparación de OCR dentro del artifact

`publish` crea siempre las carpetas:

- `artifact/publish-portable/tesseract/`
- `artifact/publish-portable/tesseract/tessdata/`
- `artifact/publish-portable/data/*`

Debes copiar los binarios reales de Tesseract dentro de esa carpeta portable:

- `tesseract/tesseract.exe`
- `tesseract/tessdata/spa.traineddata`
- `tesseract/tessdata/eng.traineddata`

## Build y tests

```bash
dotnet restore AppPortable.sln
dotnet build AppPortable.sln -c Release --no-restore
dotnet test AppPortable.Tests/AppPortable.Tests.csproj -c Release
```
