export const MAX_REQUEST_BYTES = 4 * 1024 * 1024;
export const MAX_PAGES_PER_CALL = 8;
export const MAX_CHARS_PER_CHUNK = 2_000;
export const MAX_POLL_DOCS = 50;

export const assertBodySize = (contentLength: string | null) => {
  if (!contentLength) return;
  const bytes = Number(contentLength);
  if (Number.isFinite(bytes) && bytes > MAX_REQUEST_BYTES) {
    throw new Error(`Payload too large. Max ${MAX_REQUEST_BYTES} bytes.`);
  }
};
