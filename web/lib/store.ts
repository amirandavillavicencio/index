import type { ProcessedDocument, SearchResult } from './types';

type InMemoryStore = {
  documents: Map<string, ProcessedDocument>;
};

const globalState = globalThis as typeof globalThis & { __appPortableStore?: InMemoryStore };

if (!globalState.__appPortableStore) {
  globalState.__appPortableStore = {
    documents: new Map<string, ProcessedDocument>()
  };
}

const store = globalState.__appPortableStore;

export function saveDocument(document: ProcessedDocument): void {
  store.documents.set(document.documentId, document);
}

export function listDocuments(): ProcessedDocument[] {
  return Array.from(store.documents.values());
}

export function searchDocuments(query: string, limit = 50): SearchResult[] {
  const q = query.trim().toLocaleLowerCase();
  if (!q) {
    return [];
  }

  const results: SearchResult[] = [];

  for (const document of store.documents.values()) {
    for (const chunk of document.chunks) {
      const text = chunk.text.toLocaleLowerCase();
      const idx = text.indexOf(q);
      if (idx < 0) {
        continue;
      }

      const snippetStart = Math.max(0, idx - 60);
      const snippetEnd = Math.min(chunk.text.length, idx + q.length + 120);
      const snippet = chunk.text.slice(snippetStart, snippetEnd).replace(/\s+/g, ' ').trim();

      results.push({
        chunkId: chunk.chunkId,
        documentId: chunk.documentId,
        sourceFile: chunk.sourceFile,
        pageStart: chunk.pageStart,
        pageEnd: chunk.pageEnd,
        score: idx,
        snippet
      });
    }
  }

  return results.sort((a, b) => a.score - b.score).slice(0, limit);
}
