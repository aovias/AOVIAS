import { HttpInterceptorFn } from '@angular/common/http';
import { timeout } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { JOURNEY_LONG_READ_CONTEXT } from '../http/journey-http.tokens';

/**
 * Order in `provideHttpClient(withInterceptors([...]))`:
 * loading → auth → **this** → apiResponse — timeout wraps the downstream stream including envelope unwrap.
 */
export const journeyLongReadInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.context.get(JOURNEY_LONG_READ_CONTEXT)) {
    return next(req);
  }
  const ms = environment.journeyLongReadTimeoutMs ?? 0;
  if (ms <= 0) {
    return next(req);
  }
  return next(req).pipe(timeout(ms));
};
