import { MAX_CHARS_PER_CHUNK } from './limits';
import type { DocumentChunk } from './types';

export interface ExtractedPage {
  page: number;
  text: string;
  source: 'pdf' | 'ocr';
}

export async function extractPagesWindow(params: {
  blobUrl: string;
  pageFrom: number;
  pageTo: number;
  ocrMode: 'none' | 'partial' | 'full';
}) {
  const { pageFrom, pageTo, ocrMode } = params;

  // TODO: reemplazar con parser real (pdfjs-dist o librería equivalente).
  // Este mock mantiene el contrato del pipeline asíncrono en Vercel.
  const pages: ExtractedPage[] = [];
  for (let page = pageFrom; page <= pageTo; page += 1) {
    const hasNativeText = page % 5 !== 0;
    if (hasNativeText || ocrMode === 'none') {
      pages.push({ page, text: `Texto nativo de página ${page}`, source: 'pdf' });
      continue;
    }

    if (ocrMode === 'partial' || ocrMode === 'full') {
      // OCR parcial viable dentro de Vercel: ventanas pequeñas + timeout corto.
      pages.push({ page, text: `OCR parcial de página ${page}`, source: 'ocr' });
      continue;
    }

    pages.push({ page, text: '', source: 'pdf' });
  }

  return pages;
}

export function buildChunks(documentId: string, pages: ExtractedPage[]) {
  const chunks: DocumentChunk[] = [];

  let currentText = '';
  let currentStart = pages[0]?.page ?? 1;
  let currentEnd = currentStart;

  const flush = () => {
    const text = currentText.trim();
    if (!text) return;

    const id = `${documentId}:${currentStart}-${currentEnd}:${chunks.length + 1}`;
    chunks.push({
      id,
      documentId,
      pageFrom: currentStart,
      pageTo: currentEnd,
      text,
      terms: tokenize(text),
    });

    currentText = '';
  };

  for (const page of pages) {
    if (!currentText) {
      currentStart = page.page;
      currentEnd = page.page;
    }

    const candidate = `${currentText}\n${page.text}`.trim();
    if (candidate.length > MAX_CHARS_PER_CHUNK && currentText) {
      flush();
      currentStart = page.page;
      currentEnd = page.page;
      currentText = page.text;
      continue;
    }

    currentText = candidate;
    currentEnd = page.page;
  }

  flush();
  return chunks;
}

function tokenize(text: string) {
  const unique = new Set(
    text
      .toLowerCase()
      .replace(/[^\p{L}\p{N}\s]/gu, ' ')
      .split(/\s+/)
      .filter((token) => token.length > 2),
  );

  return Array.from(unique).slice(0, 128);
}
