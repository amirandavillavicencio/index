import { NextResponse } from 'next/server';
import { MAX_PAGES_PER_CALL } from '@/lib/limits';
import { buildChunks, extractPagesWindow } from '@/lib/pipeline';
import { appendChunks, getJob, updateJob } from '@/lib/store';

export const runtime = 'nodejs';
export const maxDuration = 10;

export async function POST(request: Request) {
  const payload = (await request.json()) as {
    jobId?: string;
    pageFrom?: number;
    pageTo?: number;
  };

  if (!payload.jobId) {
    return NextResponse.json({ error: 'jobId is required' }, { status: 400 });
  }

  const job = await getJob(payload.jobId);
  if (!job) {
    return NextResponse.json({ error: 'job not found' }, { status: 404 });
  }

  if (job.status === 'completed') {
    return NextResponse.json({ job, done: true });
  }

  const start = payload.pageFrom ?? job.processedPages + 1;
  const endBound = Math.min(start + MAX_PAGES_PER_CALL - 1, job.totalPages);
  const end = Math.min(payload.pageTo ?? endBound, endBound);

  const pages = await extractPagesWindow({
    blobUrl: job.blobUrl,
    pageFrom: start,
    pageTo: end,
    ocrMode: job.ocrMode,
  });

  const chunks = buildChunks(job.documentId, pages);
  await appendChunks(job.documentId, chunks);

  const processedPages = Math.max(job.processedPages, end);
  const completed = processedPages >= job.totalPages;

  const updated = await updateJob(job.id, {
    processedPages,
    chunkCount: job.chunkCount + chunks.length,
    status: completed ? 'completed' : 'processing',
  });

  return NextResponse.json({
    job: updated,
    processedRange: [start, end],
    chunksCreated: chunks.length,
    done: completed,
    nextPageFrom: completed ? null : end + 1,
  });
}
