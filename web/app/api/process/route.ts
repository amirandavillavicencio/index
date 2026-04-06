import { NextResponse } from 'next/server';
import { getJob } from '@/lib/store';

export const runtime = 'nodejs';
export const maxDuration = 5;

/**
 * Endpoint legado del MVP.
 * Mantiene compatibilidad sin bloquear en una ejecución larga:
 * - action=start  -> delega a /api/process/start
 * - action=chunk  -> delega a /api/process/chunk
 * - action=status -> usa /api/status?jobId=
 */
export async function POST(request: Request) {
  const body = (await request.json()) as {
    action?: 'start' | 'chunk' | 'status';
    jobId?: string;
    pageFrom?: number;
    pageTo?: number;
  };

  if (!body.jobId) {
    return NextResponse.json({ error: 'jobId is required' }, { status: 400 });
  }

  const job = await getJob(body.jobId);
  if (!job) {
    return NextResponse.json({ error: 'job not found' }, { status: 404 });
  }

  switch (body.action) {
    case 'status':
      return NextResponse.json({ next: `/api/status?jobId=${body.jobId}` });
    case 'chunk':
      return NextResponse.json({
        next: '/api/process/chunk',
        payload: { jobId: body.jobId, pageFrom: body.pageFrom, pageTo: body.pageTo },
      });
    case 'start':
    default:
      return NextResponse.json({ next: '/api/process/start', payload: { jobId: body.jobId } });
  }
}
