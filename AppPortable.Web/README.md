# AppPortable.Web

Base web mínima para la migración de servicios desde la app desktop.

## Flujo soportado en esta etapa

1. Subir un PDF (`POST /api/documents`).
2. Procesar documento (extracción/chunking/indexación reutilizando servicios actuales).
3. Buscar sobre el índice (`POST /api/search`).
4. Visualizar resultados en una UI estática mínima (`wwwroot/index.html`).

## Ejecutar

```bash
dotnet run --project AppPortable.Web/AppPortable.Web.csproj
```

Luego abrir:

- `http://localhost:5000` o `https://localhost:5001` (según perfil)
- Swagger (desarrollo): `/swagger`

## Configuración de almacenamiento

Por defecto usa:

- `<bin>/AppData`

Se puede sobrescribir con:

- `Storage:RootPath` (configuración .NET)
