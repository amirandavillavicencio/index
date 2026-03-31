# Build Guide

## Prerequisites

- .NET SDK 8.0+
- Windows (required to run WPF desktop app)

## Restore

```bash
dotnet restore AppPortable.sln
```

## Build

```bash
dotnet build AppPortable.sln
```

## Run desktop app

```bash
dotnet run --project AppPortable.Desktop/AppPortable.Desktop.csproj
```

## Run tests

```bash
dotnet test AppPortable.Tests/AppPortable.Tests.csproj
```
