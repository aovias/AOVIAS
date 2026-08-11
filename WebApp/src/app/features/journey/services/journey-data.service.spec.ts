import { TestBed } from '@angular/core/testing';
import { HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { JourneyDataService } from './journey-data.service';
import { environment } from '../../../../environments/environment';
import {
  buildBulkEvidenceFormData,
  buildEvidenceVersionFormData,
  buildSingleEvidenceFormData,
} from '../utils/oe-evidence-upload.payload';
import { JOURNEY_LONG_READ_CONTEXT } from '../../../core/http/journey-http.tokens';
import { journeyLongReadInterceptor } from '../../../core/interceptors/journey-long-read.interceptor';
import { apiResponseInterceptor } from '../../../core/interceptors/api-response.interceptor';
import { TokenService } from '../../../core/auth/token.service';
import { SessionService } from '../../../core/auth/session.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';

/** Stands in for authInterceptor (avoids TokenRefreshService / HttpClient cycles in unit tests). */
const testAuthHeadersInterceptor: HttpInterceptorFn = (req, next) =>
  next(
    req.clone({
      setHeaders: {
        Authorization: 'Bearer test-jwt',
        'X-Hospital-Id': 'hospital-1',
      },
    }),
  );

const NativeXHR = globalThis.XMLHttpRequest;

/** Minimal `XMLHttpRequest` double for multipart tests (`EvidenceMultipartApiClient` uses XHR, not `HttpClient`). */
function installMultipartXhrMock(responsePayload: unknown): {
  restore: () => void;
  getLastUrl: () => string;
  getLastBody: () => FormData | undefined;
  getHeaders: () => Record<string, string>;
} {
  const headers: Record<string, string> = {};
  let lastUrl = '';
  let lastBody: FormData | undefined;

  class MockMultipartXHR {
    status = 200;
    statusText = 'OK';
    responseText = '';
    withCredentials = false;
    onload: (() => void) | null = null;
    onerror: (() => void) | null = null;

    open(_method: string, url: string | URL): void {
      lastUrl = typeof url === 'string' ? url : url.toString();
    }

    setRequestHeader(name: string, value: string): void {
      headers[name] = value;
    }

    send(body?: XMLHttpRequestBodyInit | Document | null | undefined): void {
      lastBody = body as FormData;
      this.responseText = JSON.stringify(responsePayload);
      queueMicrotask(() => this.onload?.());
    }
  }

  globalThis.XMLHttpRequest = MockMultipartXHR as unknown as typeof XMLHttpRequest;

  return {
    restore: () => {
      globalThis.XMLHttpRequest = NativeXHR;
    },
    getLastUrl: () => lastUrl,
    getLastBody: () => lastBody,
    getHeaders: () => ({ ...headers }),
  };
}

describe('JourneyDataService evidence uploads', () => {
  let service: JourneyDataService;
  let httpMock: HttpTestingController;

  const journeyId = 'j1';
  const oeId = 'oe-1';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(
          withInterceptors([
            testAuthHeadersInterceptor,
            journeyLongReadInterceptor,
            apiResponseInterceptor,
          ]),
        ),
        provideHttpClientTesting(),
        JourneyDataService,
        { provide: TokenService, useValue: { getAccessToken: () => 'test-jwt' } },
        { provide: SessionService, useValue: { getHospitalId: () => null } },
        { provide: ComplianceContextService, useValue: { getHospitalId: () => 'hospital-1' } },
      ],
    });
    service = TestBed.inject(JourneyDataService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    globalThis.XMLHttpRequest = NativeXHR;
  });

  it('uploadSingleEvidence sends FormData via XMLHttpRequest (EvidenceMultipartApiClient) to …/evidence', async () => {
    const xhrMock = installMultipartXhrMock({ success: true, data: { evidenceId: 'e1' } });
    try {
      const file = new File(['x'], 'doc.pdf', { type: 'application/pdf' });
      const fd = buildSingleEvidenceFormData({ file, evidenceType: 'Document' });
      const expectedUrl = `${environment.apiBaseUrl}/journeys/${journeyId}/objective-elements/${encodeURIComponent(oeId)}/evidence${environment.evidenceMultipartPathSuffix ?? ''}`;

      const resultPromise = firstValueFrom(service.uploadSingleEvidence(journeyId, oeId, fd));

      await expect(resultPromise).resolves.toEqual({ evidenceId: 'e1' });
      expect(xhrMock.getLastUrl()).toBe(expectedUrl);
      expect(xhrMock.getLastBody()).toBe(fd);
      const h = xhrMock.getHeaders();
      expect(h['Authorization']).toContain('Bearer');
      expect(h['api-version']).toBe('1.0');
      expect(h['X-Hospital-Id']).toBe('hospital-1');
      expect(h['X-Correlation-Id']).toBeTruthy();
      expect(Object.keys(h).some((k) => k.toLowerCase() === 'content-type')).toBe(false);
    } finally {
      xhrMock.restore();
    }
  });

  it('uploadBulkEvidence sends FormData via XMLHttpRequest to …/evidence/bulk', async () => {
    const xhrMock = installMultipartXhrMock({
      success: true,
      data: { totalFiles: 2, successCount: 2, failureCount: 0, items: [] },
    });
    try {
      const f1 = new File(['a'], 'a.pdf', { type: 'application/pdf' });
      const f2 = new File(['b'], 'b.pdf', { type: 'application/pdf' });
      const fd = buildBulkEvidenceFormData({ files: [f1, f2], evidenceType: 'Document' });
      const expectedUrl = `${environment.apiBaseUrl}/journeys/${journeyId}/objective-elements/${encodeURIComponent(oeId)}/evidence/bulk`;

      const resultPromise = firstValueFrom(service.uploadBulkEvidence(journeyId, oeId, fd));

      await expect(resultPromise).resolves.toEqual({
        totalFiles: 2,
        successCount: 2,
        failureCount: 0,
        items: [],
      });
      expect(xhrMock.getLastUrl()).toBe(expectedUrl);
      expect(xhrMock.getLastBody()).toBe(fd);
    } finally {
      xhrMock.restore();
    }
  });

  it('getObjectiveElementEvidenceDetail GETs …/evidence/{evidenceId}', () => {
    const eid = 'ev-1';
    const expectedUrl = `${environment.apiBaseUrl}/journeys/${journeyId}/objective-elements/${encodeURIComponent(oeId)}/evidence/${encodeURIComponent(eid)}`;
    service.getObjectiveElementEvidenceDetail(journeyId, oeId, eid).subscribe();
    const req = httpMock.expectOne(expectedUrl);
    expect(req.request.method).toBe('GET');
    req.flush({ id: eid, title: 'T' });
  });

  it('downloadObjectiveElementEvidenceFile GETs blob + content-type from …/download', () => {
    const eid = 'ev-1';
    const expectedUrl = `${environment.apiBaseUrl}/journeys/${journeyId}/objective-elements/${encodeURIComponent(oeId)}/evidence/${encodeURIComponent(eid)}/download`;
    service.downloadObjectiveElementEvidenceFile(journeyId, oeId, eid).subscribe((res) => {
      expect(res.blob.size).toBe(2);
      expect(res.contentType).toBe('image/png');
    });
    const req = httpMock.expectOne(expectedUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['ok']), { headers: { 'Content-Type': 'image/png' } });
  });

  it('verifyObjectiveElementEvidence POSTs to …/verify', () => {
    const eid = 'ev-1';
    const expectedUrl = `${environment.apiBaseUrl}/journeys/${journeyId}/objective-elements/${encodeURIComponent(oeId)}/evidence/${encodeURIComponent(eid)}/verify`;
    service.verifyObjectiveElementEvidence(journeyId, oeId, eid, { accepted: true }).subscribe();
    const req = httpMock.expectOne(expectedUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ accepted: true });
    req.flush({});
  });

  it('getSession marks request with journey long-read HttpContext', () => {
    const expectedUrl = `${environment.apiBaseUrl}/journeys/${journeyId}/session`;
    service.getSession(journeyId).subscribe();
    const req = httpMock.expectOne(expectedUrl);
    expect(req.request.context.get(JOURNEY_LONG_READ_CONTEXT)).toBe(true);
    req.flush({} as never);
  });

  it('uploadObjectiveElementEvidenceVersion sends FormData via XMLHttpRequest to …/versions', async () => {
    const xhrMock = installMultipartXhrMock({ success: true, data: { evidenceId: 'e2' } });
    try {
      const eid = 'ev-1';
      const file = new File(['x'], 'n.pdf', { type: 'application/pdf' });
      const fd = buildEvidenceVersionFormData({ file });
      const expectedUrl = `${environment.apiBaseUrl}/journeys/${journeyId}/objective-elements/${encodeURIComponent(oeId)}/evidence/${encodeURIComponent(eid)}/versions`;

      const resultPromise = firstValueFrom(
        service.uploadObjectiveElementEvidenceVersion(journeyId, oeId, eid, fd),
      );

      await expect(resultPromise).resolves.toEqual({ evidenceId: 'e2' });
      expect(xhrMock.getLastUrl()).toBe(expectedUrl);
      expect(xhrMock.getLastBody()).toBe(fd);
    } finally {
      xhrMock.restore();
    }
  });
});
