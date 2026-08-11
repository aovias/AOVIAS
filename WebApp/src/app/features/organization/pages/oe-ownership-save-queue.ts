import { Observable, Subject, Subscription } from 'rxjs';

export interface QueuedSaveJob<TKey extends string = string> {
  key: TKey;
  run: () => Observable<unknown>;
  onStart?: () => void;
  onSuccess?: () => void;
  onError?: (err: unknown) => void;
}

/**
 * Debounce-friendly concurrent save queue (max N in flight).
 * Newer jobs for the same key replace pending (not in-flight) jobs.
 */
export class ConcurrentSaveQueue<TKey extends string = string> {
  private readonly pending = new Map<TKey, QueuedSaveJob<TKey>>();
  private readonly inFlight = new Set<TKey>();
  private readonly subscriptions = new Map<TKey, Subscription>();
  private readonly wake$ = new Subject<void>();
  private destroyed = false;

  constructor(private readonly concurrency: number) {
    this.wake$.subscribe(() => this.pump());
  }

  enqueue(job: QueuedSaveJob<TKey>): void {
    if (this.destroyed) return;
    this.pending.set(job.key, job);
    this.wake$.next();
  }

  cancel(key: TKey): void {
    this.pending.delete(key);
  }

  destroy(): void {
    this.destroyed = true;
    this.pending.clear();
    for (const sub of this.subscriptions.values()) sub.unsubscribe();
    this.subscriptions.clear();
    this.inFlight.clear();
    this.wake$.complete();
  }

  private pump(): void {
    if (this.destroyed) return;
    while (this.inFlight.size < this.concurrency && this.pending.size > 0) {
      const nextKey = [...this.pending.keys()].find((k) => !this.inFlight.has(k));
      if (!nextKey) break;
      const job = this.pending.get(nextKey);
      if (!job) continue;
      this.pending.delete(nextKey);
      this.inFlight.add(nextKey);
      job.onStart?.();
      const sub = job.run().subscribe({
        next: () => {
          job.onSuccess?.();
        },
        error: (err) => {
          job.onError?.(err);
          this.finish(nextKey);
          this.wake$.next();
        },
        complete: () => {
          this.finish(nextKey);
          this.wake$.next();
        },
      });
      this.subscriptions.set(nextKey, sub);
    }
  }

  private finish(key: TKey): void {
    this.subscriptions.get(key)?.unsubscribe();
    this.subscriptions.delete(key);
    this.inFlight.delete(key);
  }
}
