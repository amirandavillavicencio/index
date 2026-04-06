import { put } from '@vercel/blob';
import { kv } from '@vercel/kv';
import { MAX_POLL_DOCS } from './limits';
import type { DocumentChunk, ProcessJob, SearchResult } from './types';

const jobKey = (jobId: string) => `job:${jobId}`;
const docChunksKey = (documentId: string) => `doc:${documentId}:chunks`;
const docJobsKey = 'jobs:index';

export async function savePdfToBlob(documentId: string, file: File) {
  const pathname = `pdf/${documentId}/${file.name}`;
  return put(pathname, file, { access: 'private', addRandomSuffix: true });
}

export async function createJob(job: ProcessJob) {
  await kv.set(jobKey(job.id), job);
  await kv.zadd(docJobsKey, { score: Date.now(), member: job.id });
}

export async function getJob(jobId: string) {
  return kv.get<ProcessJob>(jobKey(jobId));
}

export async function updateJob(jobId: string, patch: Partial<ProcessJob>) {
  const existing = await getJob(jobId);
  if (!existing) return null;

  const next: ProcessJob = {
    ...existing,
    ...patch,
    updatedAt: new Date().toISOString(),
  };

  await kv.set(jobKey(jobId), next);
  return next;
}

export async function appendChunks(documentId: string, chunks: DocumentChunk[]) {
  if (!chunks.length) return;
  await kv.rpush(docChunksKey(documentId), ...chunks.map((item) => JSON.stringify(item)));
}

export async function getChunks(documentId: string) {
  const raw = await kv.lrange<string[]>(docChunksKey(documentId), 0, -1);
  return raw.map((item) => JSON.parse(item) as DocumentChunk);
}

export async function listJobs(limit = MAX_POLL_DOCS) {
  const ids = await kv.zrange<string[]>(docJobsKey, -limit, -1, { rev: true });
  if (!ids.length) return [];

  const rows = await kv.mget<ProcessJob[]>(...ids.map((id) => jobKey(id)));
  return rows.filter(Boolean);
}

export async function searchChunks(query: string, limit = 20): Promise<SearchResult[]> {
  const jobs = await listJobs();
  const tokens = query.toLowerCase().split(/\s+/).filter(Boolean);
  const results: SearchResult[] = [];

  for (const job of jobs.filter((item) => item.status === 'completed')) {
    const chunks = await getChunks(job.documentId);
    for (const chunk of chunks) {
      const haystack = `${chunk.text} ${chunk.terms.join(' ')}`.toLowerCase();
      const matched = tokens.filter((token) => haystack.includes(token)).length;
      if (!matched) continue;

      const idx = haystack.indexOf(tokens[0] ?? '');
      const from = Math.max(0, idx - 80);
      const to = Math.min(chunk.text.length, idx + 200);

      results.push({
        documentId: chunk.documentId,
        chunkId: chunk.id,
        pageFrom: chunk.pageFrom,
        pageTo: chunk.pageTo,
        score: matched / Math.max(tokens.length, 1),
        snippet: chunk.text.slice(from, to),
      });
    }
  }

  return results.sort((a, b) => b.score - a.score).slice(0, limit);
}
