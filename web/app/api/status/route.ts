import { NextResponse } from 'next/server';
import { getJob, listJobs } from '@/lib/store';

export const runtime = 'nodejs';
export const maxDuration = 5;

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);
  const jobId = searchParams.get('jobId');

  if (jobId) {
    const job = await getJob(jobId);
    if (!job) {
      return NextResponse.json({ error: 'job not found' }, { status: 404 });
    }

    return NextResponse.json({ job });
  }

  const jobs = await listJobs();
  return NextResponse.json({ jobs });
}
