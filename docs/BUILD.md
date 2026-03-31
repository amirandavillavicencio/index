# Build

## Prerrequisitos

- Windows x64
- .NET SDK 8.0+

## Restore

```bash
dotnet restore AppPortable.sln
```

## Build

```bash
dotnet build AppPortable.sln -c Release --no-restore
```

## Test

```bash
dotnet test AppPortable.Tests/AppPortable.Tests.csproj -c Release --no-build
```

## Publish

```bash
dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```
