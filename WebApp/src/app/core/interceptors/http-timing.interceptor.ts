import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs';
import { PerformanceTimelineService } from '../performance/performance-timeline.service';
import { BACKGROUND_REQUEST, DASHBOARD_HTTP_CATEGORY } from '../performance/http-context.tokens';

function estimateResponseSize(body: unknown): number | null {
  if (body == null) {
    return null;
  }
  if (body instanceof Blob) {
    return body.size;
  }
  if (typeof body === 'string') {
    return body.length;
  }
  try {
    return JSON.stringify(body).length;
  } catch {
    return null;
  }
}

function classifyRequired(category: string): boolean {
  return category === 'snapshot' || category === 'control-center' || category === 'organization';
}

export const httpTimingInterceptor: HttpInterceptorFn = (req, next) => {
  const perf = inject(PerformanceTimelineService);
  if (!perf.enabled) {
    return next(req);
  }

  const startMs = performance.now();
  const category = req.context.get(DASHBOARD_HTTP_CATEGORY);
  const isBackground = req.context.get(BACKGROUND_REQUEST);
  const blocking = !isBackground && classifyRequired(category);

  return next(req).pipe(
    tap({
      next: (event) => {
        if (event instanceof HttpResponse) {
          perf.recordHttp({
            url: req.url,
            method: req.method,
            startMs,
            endMs: performance.now(),
            durationMs: performance.now() - startMs,
            responseSizeBytes: estimateResponseSize(event.body),
            blocking,
            required: classifyRequired(category),
            status: event.status,
          });
        }
      },
      error: () => {
        perf.recordHttp({
          url: req.url,
          method: req.method,
          startMs,
          endMs: performance.now(),
          durationMs: performance.now() - startMs,
          responseSizeBytes: null,
          blocking,
          required: classifyRequired(category),
          status: null,
        });
      },
    }),
  );
};
