export type JobStatus = 'queued' | 'processing' | 'completed' | 'failed';

export interface ProcessJob {
  id: string;
  documentId: string;
  filename: string;
  blobUrl: string;
  status: JobStatus;
  createdAt: string;
  updatedAt: string;
  totalPages: number;
  processedPages: number;
  chunkCount: number;
  error?: string;
  ocrMode: 'none' | 'partial' | 'full';
}

export interface DocumentChunk {
  id: string;
  documentId: string;
  pageFrom: number;
  pageTo: number;
  text: string;
  terms: string[];
}

export interface SearchResult {
  documentId: string;
  chunkId: string;
  pageFrom: number;
  pageTo: number;
  score: number;
  snippet: string;
}
