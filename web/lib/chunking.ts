import crypto from 'node:crypto';
import type { DocumentChunk } from './types';

export function computeDocumentId(fileName: string, pageTexts: string[]): string {
  const totalTextLength = pageTexts.reduce((acc, cur) => acc + cur.length, 0);
  const payload = `${fileName}|${pageTexts.length}|${totalTextLength}`;
  return crypto.createHash('sha256').update(payload).digest('hex').slice(0, 24);
}

export function createChunks(documentId: string, sourceFile: string, pages: string[]): DocumentChunk[] {
  const chunks: DocumentChunk[] = [];

  pages.forEach((pageText, pageIndex) => {
    const text = pageText.trim();
    if (!text) {
      return;
    }

    chunks.push({
      chunkId: `${documentId}-${String(chunks.length).padStart(4, '0')}`,
      documentId,
      sourceFile,
      pageStart: pageIndex + 1,
      pageEnd: pageIndex + 1,
      chunkIndex: chunks.length,
      text,
      textLength: text.length
    });
  });

  return chunks;
}
