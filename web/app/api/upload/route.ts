import { randomUUID } from 'node:crypto';
import { NextResponse } from 'next/server';
import { assertBodySize } from '@/lib/limits';
import { createJob, savePdfToBlob } from '@/lib/store';
import type { ProcessJob } from '@/lib/types';

export const runtime = 'nodejs';
export const maxDuration = 10;

export async function POST(request: Request) {
  try {
    assertBodySize(request.headers.get('content-length'));

    const formData = await request.formData();
    const file = formData.get('file');
    const ocrMode = (formData.get('ocrMode')?.toString() as ProcessJob['ocrMode']) || 'partial';
    const totalPages = Number(formData.get('totalPages') ?? 0);

    if (!(file instanceof File)) {
      return NextResponse.json({ error: 'file is required' }, { status: 400 });
    }

    const documentId = randomUUID();
    const jobId = randomUUID();

    const blob = await savePdfToBlob(documentId, file);

    const now = new Date().toISOString();
    const job: ProcessJob = {
      id: jobId,
      documentId,
      filename: file.name,
      blobUrl: blob.url,
      status: 'queued',
      createdAt: now,
      updatedAt: now,
      totalPages,
      processedPages: 0,
      chunkCount: 0,
      ocrMode,
    };

    await createJob(job);

    return NextResponse.json({
      jobId,
      documentId,
      status: job.status,
    });
  } catch (error) {
    return NextResponse.json(
      {
        error: error instanceof Error ? error.message : 'upload failed',
      },
      { status: 500 },
    );
  }
}
