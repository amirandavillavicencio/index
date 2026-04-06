# Migración de AppPortable (WPF/.NET) a Web desplegable en Vercel

## Objetivo

Migrar de desktop (WPF) a web sin perder la lógica de negocio crítica, con una arquitectura mínima viable para producción y una demo funcional de:

1. Subir PDF.
2. Procesar texto.
3. Buscar.
4. Mostrar resultados.

## 1) Qué reutilizar del proyecto actual

### Reutilizable sin cambios

- **Modelos y contratos (`AppPortable.Core`)**: conceptos de `ProcessedDocument`, `DocumentChunk`, `SearchResult`, pipeline, etc.
- **Lógica de pipeline (`AppPortable.Infrastructure`)**: extracción, chunking, persistencia, indexación.
- **Pruebas (`AppPortable.Tests`)**: reglas de chunking, pipeline y búsqueda como referencia de comportamiento esperado.

### No reutilizable directamente en Vercel

- **UI WPF (`AppPortable.Desktop`)**: no corre en navegador.
- **Dependencias nativas de escritorio/Windows**.
- **Persistencia local de SQLite/JSON en filesystem local** como almacenamiento durable en serverless.

## 2) Separación de capas recomendada

- Mantener `Core + Infrastructure + Search` como **motor de dominio**.
- Tratar `Desktop` como adaptador legacy.
- Agregar adaptador web:
  - **Frontend**: Next.js (`web/app/page.tsx`).
  - **Backend HTTP**: Next.js API routes (`web/app/api/*`) para MVP.

## 3) Decisión de backend para Vercel

### Opción elegida para este MVP

- **Backend en rutas/API de Next.js (Node runtime)**.
- Motivo: despliegue directo en Vercel, menor fricción para demo funcional.

### Opción para producción robusta

- Mantener backend en **ASP.NET Core** si se prioriza reutilización 1:1 del motor C#.
- En ese caso:
  - Vercel hospeda frontend Next.js.
  - API ASP.NET Core se despliega fuera de Vercel (Azure App Service, Container Apps, Fly.io, etc.).
  - Frontend consume API remota.

## 4) Implementación inicial creada

Se agregó un proyecto `web/` con:

- `web/app/page.tsx`: UI de carga/proceso/búsqueda.
- `web/app/api/process/route.ts`: recibe PDF en base64, extrae texto y genera chunks.
- `web/app/api/search/route.ts`: búsqueda full-text simple sobre chunks indexados en memoria.
- `web/lib/*`: tipos, chunking, extracción PDF y store en memoria.

## 5) Limitaciones actuales del MVP (importante)

1. **Persistencia efímera**: el índice vive en memoria del proceso Node.
2. **No OCR real en la versión web inicial**.
3. **Sin SQLite FTS5 serverless durable** por defecto.
4. **Límites de tamaño/tiempo de funciones serverless** para PDFs grandes.

## 6) Etapas de migración sugeridas

### Etapa 1 (actual, demo funcional)

- Next.js con API routes.
- Upload PDF → extracción → chunking → búsqueda en memoria.

### Etapa 2 (persistencia y producción)

- Persistencia durable en Vercel Postgres / Neon / Supabase.
- Índice de búsqueda durable (PostgreSQL full-text o motor externo).
- Metadata + blobs de PDF en Vercel Blob/S3.

### Etapa 3 (paridad con desktop)

- OCR real como servicio separado.
- Reindexación asíncrona por cola.
- Observabilidad y auditoría.

## 7) Instrucciones exactas para deploy en Vercel

> Ruta de proyecto a desplegar: `web/`.

### 7.1 Preparación local

```bash
cd web
npm install
npm run build
```

### 7.2 Deploy con Vercel CLI

```bash
npm i -g vercel
cd web
vercel login
vercel
```

Respuestas recomendadas cuando pregunte el asistente:

- Set up and deploy? **Yes**
- Which scope? **tu cuenta/equipo**
- Link to existing project? **No** (la primera vez)
- Project name: **appportable-web**
- In which directory is your code located? **.**

Para producción:

```bash
vercel --prod
```

### 7.3 Deploy desde GitHub

1. Subir rama a GitHub.
2. En Vercel: **Add New Project**.
3. Importar repo.
4. En **Root Directory** seleccionar `web`.
5. Build command: `npm run build`.
6. Output directory: `.next` (automático en Next.js).
7. Deploy.

## 8) Riesgos de compatibilidad con Vercel

- **ASP.NET Core no despliega nativamente como runtime principal en Vercel**.
- **SQLite local y filesystem local no son almacenamiento durable en serverless**.
- **OCR con binarios nativos** puede requerir contenedores/infra externa.

## 9) Garantía de no ruptura del desktop

Esta migración inicial agrega `web/` y documentación, sin modificar proyectos existentes de `AppPortable.Desktop`, `Core`, `Infrastructure`, `Search` ni sus tests.
