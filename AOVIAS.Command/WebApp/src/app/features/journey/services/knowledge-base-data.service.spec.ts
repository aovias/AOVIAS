import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { KnowledgeBaseDataService } from './knowledge-base-data.service';
import { environment } from '../../../../environments/environment';
import { ObjectiveElement } from '../models/objective-element.model';
import { NodeDetailsDto } from '../models/knowledge-base-api.model';

describe('KnowledgeBaseDataService', () => {
  let service: KnowledgeBaseDataService;
  let httpMock: HttpTestingController;

  const kbItem = {
    id: '1',
    nodeId: 'n1',
    explanation: 'x',
    whyItMatters: '',
    howToComply: '',
    evidenceRequired: '',
    auditChecklist: '',
    riskLevel: 'Low',
    impactLevel: 'Low',
    tags: '',
    version: 1,
    isActive: true,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [KnowledgeBaseDataService],
    });
    service = TestBed.inject(KnowledgeBaseDataService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getNodeDetailsByCode requests GET …/nodes/code/{nodeCode}/details', () => {
    const nodeCode = 'AAC.1.a';
    const expectedUrl = `${environment.apiBaseUrl}/${environment.knowledgeBasePath}/nodes/code/${encodeURIComponent(nodeCode)}/details`;

    const payload: NodeDetailsDto = {
      node: null,
      requirement: null,
      knowledge: [],
      requirementScore: null,
    };

    service.getNodeDetailsByCode(nodeCode).subscribe((res) => {
      expect(res).toEqual(payload);
    });

    const req = httpMock.expectOne(expectedUrl);
    expect(req.request.method).toBe('GET');
    req.flush(payload);
  });

  it('resolveNodeDetailsForObjectiveElement calls details by code first when data is bindable', () => {
    const oe: ObjectiveElement = {
      id: 'b7cf29c6-2d92-41e1-b754-0035bddf8d6e',
      standardId: 's1',
      code: 'AAC.1.a',
      description: 'd',
      classification: 'core',
      score: 3,
      scoreLabel: 'Partial',
      evidenceStatus: 'missing',
      isNa: false,
      auditStatus: 'compliant',
      actionPlans: [],
      evidence: [],
      history: [],
    };

    const detailsUrl = `${environment.apiBaseUrl}/${environment.knowledgeBasePath}/nodes/code/${encodeURIComponent(oe.code)}/details`;

    service.resolveNodeDetailsForObjectiveElement(oe).subscribe((res) => {
      expect(res?.knowledge?.length).toBe(1);
      expect(res?.knowledge?.[0].explanation).toBe('x');
    });

    const d1 = httpMock.expectOne(detailsUrl);
    expect(d1.request.method).toBe('GET');
    d1.flush({
      node: null,
      requirement: null,
      knowledge: [kbItem],
      requirementScore: null,
    });
  });

  it('resolveNodeDetailsForObjectiveElement falls back to knowledge list when details has no bindable content', () => {
    const oe: ObjectiveElement = {
      id: 'missing-node',
      standardId: 's1',
      code: 'PRE.1',
      description: 'd',
      classification: 'core',
      score: 3,
      scoreLabel: 'Partial',
      evidenceStatus: 'missing',
      isNa: false,
      auditStatus: 'compliant',
      actionPlans: [],
      evidence: [],
      history: [],
    };

    const detailsUrl = `${environment.apiBaseUrl}/${environment.knowledgeBasePath}/nodes/code/${encodeURIComponent(oe.code)}/details`;
    const listUrl = `${environment.apiBaseUrl}/${environment.knowledgeBasePath}/knowledge/node/code/${encodeURIComponent(oe.code)}`;

    service.resolveNodeDetailsForObjectiveElement(oe).subscribe((res) => {
      expect(res?.knowledge?.length).toBe(1);
      expect(res?.knowledge?.[0].explanation).toBe('from-list');
    });

    httpMock.expectOne(detailsUrl).flush({
      node: null,
      requirement: null,
      knowledge: [],
      requirementScore: null,
    });
    httpMock.expectOne(listUrl).flush([
      {
        ...kbItem,
        explanation: 'from-list',
      },
    ]);
  });

  it('resolveNodeDetailsForObjectiveElement falls back to list when details returns 404', () => {
    const oe: ObjectiveElement = {
      id: 'id1',
      standardId: 's1',
      code: 'PRE.1',
      description: 'd',
      classification: 'core',
      score: 3,
      scoreLabel: 'Partial',
      evidenceStatus: 'missing',
      isNa: false,
      auditStatus: 'compliant',
      actionPlans: [],
      evidence: [],
      history: [],
    };

    const detailsUrl = `${environment.apiBaseUrl}/${environment.knowledgeBasePath}/nodes/code/${encodeURIComponent(oe.code)}/details`;
    const listUrl = `${environment.apiBaseUrl}/${environment.knowledgeBasePath}/knowledge/node/code/${encodeURIComponent(oe.code)}`;

    service.resolveNodeDetailsForObjectiveElement(oe).subscribe((res) => {
      expect(res?.knowledge?.[0].explanation).toBe('y');
    });

    httpMock.expectOne(detailsUrl).flush('err', { status: 404, statusText: 'Not Found' });
    httpMock.expectOne(listUrl).flush([{ ...kbItem, explanation: 'y' }]);
  });

  it('resolveNodeDetailsForObjectiveElement propagates 500 from details', () => {
    const oe: ObjectiveElement = {
      id: 'id1',
      standardId: 's1',
      code: 'PRE.1',
      description: 'd',
      classification: 'core',
      score: 3,
      scoreLabel: 'Partial',
      evidenceStatus: 'missing',
      isNa: false,
      auditStatus: 'compliant',
      actionPlans: [],
      evidence: [],
      history: [],
    };

    const detailsUrl = `${environment.apiBaseUrl}/${environment.knowledgeBasePath}/nodes/code/${encodeURIComponent(oe.code)}/details`;

    return new Promise<void>((resolve, reject) => {
      service.resolveNodeDetailsForObjectiveElement(oe).subscribe({
        next: () => reject(new Error('expected HTTP error')),
        error: (e: HttpErrorResponse) => {
          expect(e.status).toBe(500);
          resolve();
        },
      });

      httpMock.expectOne(detailsUrl).flush('err', { status: 500, statusText: 'Server Error' });
    });
  });
});
