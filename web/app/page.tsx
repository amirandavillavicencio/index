'use client';

import { FormEvent, useEffect, useMemo, useState } from 'react';

type Job = {
  id: string;
  filename: string;
  status: 'queued' | 'processing' | 'completed' | 'failed';
  processedPages: number;
  totalPages: number;
  chunkCount: number;
};

type SearchResult = {
  documentId: string;
  chunkId: string;
  pageFrom: number;
  pageTo: number;
  score: number;
  snippet: string;
};

export default function HomePage() {
  const [jobs, setJobs] = useState<Job[]>([]);
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchResult[]>([]);

  useEffect(() => {
    const timer = setInterval(async () => {
      const response = await fetch('/api/status', { cache: 'no-store' });
      if (!response.ok) return;
      const data = await response.json();
      setJobs(data.jobs ?? []);
    }, 2000);

    return () => clearInterval(timer);
  }, []);

  const sorted = useMemo(() => jobs.slice().sort((a, b) => a.filename.localeCompare(b.filename)), [jobs]);

  const onSearch = async (event: FormEvent) => {
    event.preventDefault();
    const response = await fetch('/api/search', {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ query }),
    });

    if (!response.ok) return;
    const data = await response.json();
    setResults(data.results ?? []);
  };

  return (
    <main style={{ padding: 24, fontFamily: 'sans-serif', display: 'grid', gap: 20 }}>
      <section>
        <h2>Documentos</h2>
        {sorted.length === 0 ? <p>Sin documentos todavía.</p> : null}
        <ul>
          {sorted.map((job) => {
            const progress = job.totalPages ? Math.round((job.processedPages / job.totalPages) * 100) : 0;
            return (
              <li key={job.id}>
                <strong>{job.filename}</strong> · {job.status} · {progress}% · {job.chunkCount} chunks
              </li>
            );
          })}
        </ul>
      </section>

      <section>
        <h2>Búsqueda</h2>
        <form onSubmit={onSearch} style={{ display: 'flex', gap: 8 }}>
          <input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Buscar..." />
          <button type="submit">Buscar</button>
        </form>

        <ul>
          {results.map((item) => (
            <li key={item.chunkId}>
              <strong>{item.documentId}</strong> p.{item.pageFrom}-{item.pageTo} · score {item.score.toFixed(2)}
              <div>{item.snippet}</div>
            </li>
          ))}
        </ul>
      </section>
    </main>
  );
}
