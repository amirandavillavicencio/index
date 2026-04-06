import { NextResponse } from 'next/server';
import { z } from 'zod';
import { searchDocuments } from '@/lib/store';

export const runtime = 'nodejs';

const searchSchema = z.object({
  query: z.string().min(1),
  limit: z.number().int().positive().max(200).optional()
});

export async function POST(request: Request): Promise<NextResponse> {
  const payload = searchSchema.safeParse(await request.json());
  if (!payload.success) {
    return NextResponse.json({ error: 'Payload inválido.' }, { status: 400 });
  }

  const { query, limit = 50 } = payload.data;
  const results = searchDocuments(query, limit);

  return NextResponse.json({
    total: results.length,
    results
  });
}
