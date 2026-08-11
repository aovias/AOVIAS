import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../../environments/environment';

export interface PerformanceMarkEntry {
  name: string;
  timestampMs: number;
  detail?: Record<string, unknown>;
}

export interface PerformanceMeasureEntry {
  name: string;
  durationMs: number;
  startMark: string;
  endMark: string;
}

export interface HttpTimingEntry {
  url: string;
  method: string;
  startMs: number;
  endMs: number;
  durationMs: number;
  responseSizeBytes: number | null;
  blocking: boolean;
  required: boolean;
  status: number | null;
}

export interface PerformanceReport {
  generatedAt: string;
  marks: PerformanceMarkEntry[];
  measures: PerformanceMeasureEntry[];
  httpRequests: HttpTimingEntry[];
  webVitals: Record<string, number | null>;
}

declare global {
  interface Window {
    __AOVIAS_PERF_REPORT__?: PerformanceReport;
  }
}

@Injectable({ providedIn: 'root' })
export class PerformanceTimelineService {
  private readonly isBrowser: boolean;
  private readonly marks = new Map<string, PerformanceMarkEntry>();
  private readonly measures: PerformanceMeasureEntry[] = [];
  private readonly httpRequests: HttpTimingEntry[] = [];
  private readonly webVitals: Record<string, number | null> = {
    fcp: null,
    lcp: null,
  };
  private originMs = 0;

  constructor() {
    this.isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    if (this.isBrowser && environment.enablePerformanceTimeline) {
      this.originMs = performance.timeOrigin;
      this.observeWebVitals();
    }
  }

  get enabled(): boolean {
    return this.isBrowser && environment.enablePerformanceTimeline;
  }

  mark(name: string, detail?: Record<string, unknown>): void {
    if (!this.enabled) {
      return;
    }
    const timestampMs = performance.now();
    this.marks.set(name, { name, timestampMs, detail });
    try {
      performance.mark(name, { detail: detail as PerformanceMarkOptions['detail'] });
    } catch {
      // ignore duplicate mark names in strict mode
    }
  }

  measure(name: string, startMark: string, endMark: string): void {
    if (!this.enabled) {
      return;
    }
    const start = this.marks.get(startMark);
    const end = this.marks.get(endMark);
    if (!start || !end) {
      return;
    }
    const durationMs = end.timestampMs - start.timestampMs;
    this.measures.push({ name, durationMs, startMark, endMark });
    try {
      performance.measure(name, startMark, endMark);
    } catch {
      // ignore if marks missing in Performance API
    }
  }

  recordHttp(entry: HttpTimingEntry): void {
    if (!this.enabled) {
      return;
    }
    this.httpRequests.push(entry);
  }

  exportReport(): PerformanceReport {
    const report: PerformanceReport = {
      generatedAt: new Date().toISOString(),
      marks: [...this.marks.values()].sort((a, b) => a.timestampMs - b.timestampMs),
      measures: [...this.measures],
      httpRequests: [...this.httpRequests],
      webVitals: { ...this.webVitals },
    };
    if (this.isBrowser) {
      window.__AOVIAS_PERF_REPORT__ = report;
      const phaseRows = [
        {
          event: 'Shell Rendered',
          durationMs: this.measureDuration('dashboard_route_activate', 'dashboard_shell_rendered'),
        },
        {
          event: 'Phase 1',
          durationMs: this.measureDuration(
            'dashboard_route_activate',
            'dashboard_phase_1_complete',
          ),
        },
        {
          event: 'Phase 2',
          durationMs: this.measureDuration(
            'dashboard_route_activate',
            'dashboard_phase_2_complete',
          ),
        },
        {
          event: 'Phase 3',
          durationMs: this.measureDuration(
            'dashboard_route_activate',
            'dashboard_phase_3_complete',
          ),
        },
        {
          event: 'Snapshot Received',
          durationMs: this.measureDuration('dashboard_route_activate', 'snapshot_received'),
        },
        {
          event: 'Dashboard Interactive',
          durationMs: this.measureDuration('dashboard_route_activate', 'dashboard_interactive'),
        },
      ].filter((r) => r.durationMs != null);
      if (phaseRows.length) {
        console.table(phaseRows);
      }
      console.table(
        report.measures.map((m) => ({ measure: m.name, durationMs: Math.round(m.durationMs) })),
      );
      console.table(
        report.httpRequests.map((r) => ({
          url: r.url,
          durationMs: Math.round(r.durationMs),
          blocking: r.blocking,
          required: r.required,
        })),
      );
    }
    return report;
  }

  private measureDuration(startMark: string, endMark: string): number | null {
    const start = this.marks.get(startMark);
    const end = this.marks.get(endMark);
    if (!start || !end) {
      return null;
    }
    return Math.round(end.timestampMs - start.timestampMs);
  }

  private observeWebVitals(): void {
    try {
      const po = new PerformanceObserver((list) => {
        for (const entry of list.getEntries()) {
          if (entry.name === 'first-contentful-paint') {
            this.webVitals['fcp'] = entry.startTime;
            this.mark('first_contentful_paint');
          }
          if (entry.entryType === 'largest-contentful-paint') {
            this.webVitals['lcp'] = entry.startTime;
            this.mark('largest_contentful_paint');
          }
        }
      });
      po.observe({ type: 'paint', buffered: true });
      po.observe({ type: 'largest-contentful-paint', buffered: true });
    } catch {
      // PerformanceObserver not supported
    }
  }
}
