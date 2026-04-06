'use client';

import { useMemo, useState } from 'react';

type ProcessResponse = {
  documentId: string;
  fileName: string;
  totalPages: number;
  chunkCount: number;
  warnings: string[];
};

type SearchResult = {
  chunkId: string;
  documentId: string;
  sourceFile: string;
  pageStart: number;
  pageEnd: number;
  score: number;
  snippet: string;
};

export default function HomePage() {
  const [file, setFile] = useState<File | null>(null);
  const [query, setQuery] = useState('');
  const [processResult, setProcessResult] = useState<ProcessResponse | null>(null);
  const [searchResults, setSearchResults] = useState<SearchResult[]>([]);
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState('Listo para procesar.');

  const canSearch = useMemo(() => query.trim().length > 0, [query]);

  async function toBase64(targetFile: File): Promise<string> {
    const arr = await targetFile.arrayBuffer();
    const bytes = new Uint8Array(arr);
    let binary = '';
    for (const b of bytes) {
      binary += String.fromCharCode(b);
    }
    return btoa(binary);
  }

  async function processPdf() {
    if (!file) {
      setStatus('Selecciona un PDF primero.');
      return;
    }

    setBusy(true);
    setStatus(`Procesando ${file.name}...`);

    try {
      const contentBase64 = await toBase64(file);

      const response = await fetch('/api/process', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ fileName: file.name, contentBase64 })
      });

      const data = await response.json();
      if (!response.ok) {
        throw new Error(data.error ?? 'Error procesando PDF.');
      }

      setProcessResult(data as ProcessResponse);
      setStatus(`Documento ${file.name} procesado correctamente.`);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Error inesperado.';
      setStatus(message);
    } finally {
      setBusy(false);
    }
  }

  async function search() {
    if (!canSearch) {
      return;
    }

    setBusy(true);
    setStatus(`Buscando '${query}'...`);

    try {
      const response = await fetch('/api/search', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ query, limit: 20 })
      });

      const data = await response.json();
      if (!response.ok) {
        throw new Error(data.error ?? 'Error buscando.');
      }

      setSearchResults(data.results as SearchResult[]);
      setStatus(`Búsqueda completada: ${data.total} resultado(s).`);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Error inesperado.';
      setStatus(message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <main>
      <h1>AppPortable Web (MVP en Next.js)</h1>
      <p className="muted">
        Demo funcional: subir PDF, extraer texto, indexar en memoria y buscar snippets.
      </p>

      <section>
        <h2>1) Cargar y procesar PDF</h2>
        <div className="row">
          <input
            type="file"
            accept="application/pdf"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            disabled={busy}
          />
          <button type="button" onClick={processPdf} disabled={busy || !file}>
            Procesar
          </button>
        </div>

        {processResult ? (
          <pre>
            {JSON.stringify(processResult, null, 2)}
          </pre>
        ) : (
          <p className="muted">Sin documento procesado aún.</p>
        )}
      </section>

      <section>
        <h2>2) Buscar</h2>
        <div className="row">
          <input
            type="text"
            placeholder="Ej: contrato, cláusula, monto"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            disabled={busy}
          />
          <button type="button" onClick={search} disabled={busy || !canSearch}>
            Buscar
          </button>
        </div>

        {searchResults.length === 0 ? (
          <p className="muted">Sin resultados todavía.</p>
        ) : (
          searchResults.map((result) => (
            <article className="result" key={result.chunkId}>
              <strong>{result.sourceFile}</strong>
              <div className="muted">Página {result.pageStart} · score {result.score}</div>
              <p>{result.snippet}</p>
            </article>
          ))
        )}
      </section>

      <p className="muted">Estado: {status}</p>
    </main>
  );
}
