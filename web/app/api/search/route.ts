import { NextResponse } from 'next/server';
import { searchChunks } from '@/lib/store';

export const runtime = 'nodejs';
export const maxDuration = 5;

export async function POST(request: Request) {
  const body = (await request.json()) as { query?: string; limit?: number };
  const query = body.query?.trim();

  if (!query) {
    return NextResponse.json({ error: 'query is required' }, { status: 400 });
  }

  const results = await searchChunks(query, body.limit ?? 20);
  return NextResponse.json({ query, total: results.length, results });
}
