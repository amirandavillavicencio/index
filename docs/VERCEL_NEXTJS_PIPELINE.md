# Arquitectura Vercel + Next.js para pipeline PDF/OCR/chunking/index/search

## Objetivo
Evolucionar el MVP existente (`/api/process`, `/api/search`, chunking básico, store en memoria) a un diseño totalmente serverless que funcione dentro de límites de Vercel.

## Decisiones de arquitectura

- **Persistencia**: mover estado en memoria a **Vercel KV** (jobs, progreso, chunks y metadatos de búsqueda).
- **Archivo PDF**: guardar el binario en **Vercel Blob**; el backend solo procesa ventanas de páginas.
- **Procesamiento**: pipeline asíncrono en pasos pequeños (máx. unos segundos por request).
- **Control de flujo**: frontend hace polling (`/api/status`) y dispara siguientes pasos (`/api/process/chunk`).
- **Compatibilidad MVP**: mantener `/api/process` como endpoint legado, pero convertido en enrutador hacia nuevos endpoints.

## Flujo end-to-end

1. `POST /api/upload`
   - Recibe `file`, `totalPages`, `ocrMode`.
   - Sube a Blob.
   - Crea `job` en KV con estado `queued`.
2. `POST /api/process/start`
   - Cambia job a `processing`.
3. `POST /api/process/chunk`
   - Procesa una ventana de páginas (`MAX_PAGES_PER_CALL`, por ejemplo 8).
   - Extrae texto/OCR parcial.
   - Genera y persiste chunks.
   - Actualiza progreso `processedPages`.
   - Si llega al final: `status=completed`.
4. `GET /api/status?jobId=...` o `GET /api/status`
   - Devuelve estado puntual o lista de jobs para dashboard.
5. `POST /api/search`
   - Busca sobre chunks persistidos.

## Estrategia OCR realista en Vercel

- **Modo recomendado por defecto**: `partial`.
- Regla:
  - si página trae texto nativo PDF: usarlo (rápido, barato).
  - si no trae texto: OCR **solo de esa página** y en ventanas pequeñas.
- Si OCR completo es costoso para ciertos PDFs:
  - marcar job con `ocrMode=none` (texto nativo solamente), o
  - reintentos de OCR diferido por páginas faltantes.

## Límites y protección

- Validar `content-length` para evitar payloads > 4 MB por request API.
- Nunca cargar PDF completo en memoria en endpoints de proceso.
- Limitar páginas por llamada (`MAX_PAGES_PER_CALL`).
- Limitar tamaño de chunk (`MAX_CHARS_PER_CHUNK`).
- Configurar `maxDuration` corto por route.

## Endpoints

- `POST /api/upload`
- `POST /api/process/start`
- `POST /api/process/chunk`
- `GET /api/status`
- `POST /api/search`
- `POST /api/process` (compatibilidad MVP; delega)

## Frontend

- Polling cada 2 segundos contra `/api/status`.
- Lista de documentos con progreso `%` y estado.
- Formulario de búsqueda con resultados por chunk (score, snippet, páginas).

## Migración incremental desde MVP

1. Sustituir `web/lib/store.ts` en memoria por adaptador KV/Blob.
2. Añadir nuevos routes asíncronos (`upload/start/chunk/status`).
3. Convertir `web/app/api/process/route.ts` a wrapper legado.
4. Mantener `web/app/api/search/route.ts`, pero leyendo de KV.
5. Actualizar UI para polling + control de job.
