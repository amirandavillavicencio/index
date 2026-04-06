import { NextResponse } from 'next/server';
import { getJob, updateJob } from '@/lib/store';

export const runtime = 'nodejs';
export const maxDuration = 5;

export async function POST(request: Request) {
  const { jobId } = (await request.json()) as { jobId?: string };

  if (!jobId) {
    return NextResponse.json({ error: 'jobId is required' }, { status: 400 });
  }

  const current = await getJob(jobId);
  if (!current) {
    return NextResponse.json({ error: 'job not found' }, { status: 404 });
  }

  if (current.status === 'completed') {
    return NextResponse.json({ job: current, nextAction: 'done' });
  }

  const job = await updateJob(jobId, { status: 'processing' });

  return NextResponse.json({
    job,
    nextAction: 'call /api/process/chunk until completed',
  });
}
