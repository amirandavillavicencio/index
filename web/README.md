# AppPortable Web (Next.js)

MVP web para demo de migración desde AppPortable Desktop.

## Qué hace

- Carga un PDF.
- Extrae texto.
- Genera chunks por página.
- Indexa en memoria.
- Permite búsqueda con snippets.

## Ejecutar local

```bash
npm install
npm run dev
```

Abrir `http://localhost:3000`.

## Deploy en Vercel

Ver instrucciones detalladas en `docs/MIGRATION_TO_VERCEL.md`.

## Limitaciones

- Persistencia en memoria (efímera).
- Sin OCR real.
- No sustituye todavía al pipeline C# completo de producción.
