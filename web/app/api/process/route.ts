import { NextResponse } from 'next/server';
import { z } from 'zod';
import { computeDocumentId, createChunks } from '@/lib/chunking';
import { extractPdfPages } from '@/lib/pdf';
import { saveDocument } from '@/lib/store';

export const runtime = 'nodejs';

const processSchema = z.object({
  fileName: z.string().min(1),
  contentBase64: z.string().min(1)
});

export async function POST(request: Request): Promise<NextResponse> {
  const payload = processSchema.safeParse(await request.json());
  if (!payload.success) {
    return NextResponse.json({ error: 'Payload inválido.' }, { status: 400 });
  }

  const { fileName, contentBase64 } = payload.data;

  if (!fileName.toLocaleLowerCase().endsWith('.pdf')) {
    return NextResponse.json({ error: 'Solo se permiten archivos PDF.' }, { status: 400 });
  }

  const buffer = Buffer.from(contentBase64, 'base64');
  const pages = await extractPdfPages(buffer);

  if (pages.length === 0) {
    return NextResponse.json({ error: 'No se extrajo texto del PDF.' }, { status: 422 });
  }

  const documentId = computeDocumentId(fileName, pages);
  const chunks = createChunks(documentId, fileName, pages);

  saveDocument({
    documentId,
    fileName,
    totalPages: pages.length,
    processedAt: new Date().toISOString(),
    chunks
  });

  return NextResponse.json({
    documentId,
    fileName,
    totalPages: pages.length,
    chunkCount: chunks.length,
    warnings: [
      'Persistencia en memoria para demo. En Vercel es efímera entre ejecuciones.'
    ]
  });
}
