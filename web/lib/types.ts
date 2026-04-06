export type DocumentChunk = {
  chunkId: string;
  documentId: string;
  sourceFile: string;
  pageStart: number;
  pageEnd: number;
  chunkIndex: number;
  text: string;
  textLength: number;
};

export type ProcessedDocument = {
  documentId: string;
  fileName: string;
  totalPages: number;
  processedAt: string;
  chunks: DocumentChunk[];
};

export type SearchResult = {
  chunkId: string;
  documentId: string;
  sourceFile: string;
  pageStart: number;
  pageEnd: number;
  score: number;
  snippet: string;
};
