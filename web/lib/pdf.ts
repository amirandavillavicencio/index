import pdf from 'pdf-parse';

const formFeed = /\f/g;

export async function extractPdfPages(buffer: Buffer): Promise<string[]> {
  const parsed = await pdf(buffer);
  const normalizedText = (parsed.text ?? '').replace(/\r\n/g, '\n');

  if (!normalizedText.trim()) {
    return [];
  }

  const byFormFeed = normalizedText
    .split(formFeed)
    .map((part) => part.trim())
    .filter((part) => part.length > 0);

  if (byFormFeed.length > 0) {
    return byFormFeed;
  }

  return [normalizedText.trim()];
}
