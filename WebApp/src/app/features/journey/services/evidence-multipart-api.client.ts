import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TokenService } from '../../../core/auth/token.service';
import { SessionService } from '../../../core/auth/session.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';

/**
 * Dedicated client for **multipart** evidence uploads only.
 *
 * Uses **`XMLHttpRequest`** + `FormData` so the browser serializes file parts with real bytes.
 * Does **not** use `HttpClient` (avoids stack quirks) and does **not** set `Content-Type`
 * (the runtime sets `multipart/form-data` + boundary).
 *
 * Auth + hospital IDs are set here; **`apiResponseInterceptor` does not run** — success/error
 * envelopes are unwrapped in {@link unwrapApiEnvelope} to match other APIs’ shapes.
 */
@Injectable({
  providedIn: 'root',
})
export class EvidenceMultipartApiClient {
  private readonly tokenService = inject(TokenService);
  private readonly sessionService = inject(SessionService);
  private readonly complianceContext = inject(ComplianceContextService);

  postFormData<T>(url: string, formData: FormData): Observable<T> {
    return new Observable<T>((subscriber) => {
      if (!this.formDataHasNonEmptyBinaryParts(formData)) {
        subscriber.error(
          new HttpErrorResponse({
            error: { message: 'A non-empty file is required (client check).' },
            status: 400,
            statusText: 'Validation Failed',
            url,
          }),
        );
        return;
      }

      const xhr = new XMLHttpRequest();
      xhr.open('POST', url);
      xhr.withCredentials = true;

      const token = this.tokenService.getAccessToken();
      if (token) {
        xhr.setRequestHeader('Authorization', `Bearer ${token}`);
      }
      xhr.setRequestHeader('X-Correlation-Id', crypto.randomUUID());
      xhr.setRequestHeader('api-version', '1.0');
      const hid = this.complianceContext.getHospitalId() ?? this.sessionService.getHospitalId();
      if (hid !== undefined && hid !== null && hid !== '') {
        xhr.setRequestHeader('X-Hospital-Id', String(hid));
      }

      xhr.onload = (): void => {
        const text = xhr.responseText ?? '';
        let parsed: unknown;
        try {
          parsed = text.length ? JSON.parse(text) : null;
        } catch {
          parsed = text;
        }
        if (xhr.status < 200 || xhr.status >= 300) {
          subscriber.error(
            new HttpErrorResponse({
              error: parsed,
              status: xhr.status,
              statusText: xhr.statusText,
              url,
            }),
          );
          return;
        }
        try {
          subscriber.next(this.unwrapApiEnvelope<T>(parsed, url));
          subscriber.complete();
        } catch (e) {
          subscriber.error(e);
        }
      };

      xhr.onerror = (): void =>
        subscriber.error(
          new HttpErrorResponse({
            error: new Error('Network error'),
            status: 0,
            statusText: 'Unknown Error',
            url,
          }),
        );

      xhr.send(formData);
    });
  }

  /**
   * Ensures there is at least one `file` / `files` part and each is a non-empty Blob.
   */
  private formDataHasNonEmptyBinaryParts(fd: FormData): boolean {
    let anyPart = false;
    let allNonEmpty = true;
    fd.forEach((value, key) => {
      if (key !== 'file' && key !== 'files') return;
      anyPart = true;
      if (typeof value === 'string' || !(value instanceof Blob) || value.size <= 0) {
        allNonEmpty = false;
      }
    });
    return anyPart && allNonEmpty;
  }

  /** Same rules as `apiResponseInterceptor` + tests without that interceptor. */
  private unwrapApiEnvelope<T>(body: unknown, url: string): T {
    if (body && typeof body === 'object' && 'success' in body) {
      const api = body as { success: boolean; data?: T; message?: string };
      if (api.success === true && api.data !== undefined) {
        return api.data;
      }
      if (api.success === false) {
        throw new HttpErrorResponse({
          error: body,
          status: 400,
          statusText: api.message ?? 'Request failed',
          url,
        });
      }
    }
    return body as T;
  }
}
