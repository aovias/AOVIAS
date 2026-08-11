import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { environment } from '../../../../environments/environment';
import {
  AiAskRequestDto,
  AiAskResponseDto,
  KnowledgeItemDto,
  NodeDetailsDto,
} from '../models/knowledge-base-api.model';
import { ObjectiveElement } from '../models/objective-element.model';
import {
  syntheticNodeDetailsFromKnowledgeItems,
  hasBindableNodeDetailsContent,
} from '../utils/knowledge-base-oe-wizard.mapper';

@Injectable({
  providedIn: 'root',
})
export class KnowledgeBaseDataService extends BaseApiService {
  private kb(path: string): string {
    const seg = environment.knowledgeBasePath ?? 'knowledge-base';
    const p = path.startsWith('/') ? path : `/${path}`;
    return `/${seg}${p}`;
  }

  /**
   * `GET …/nodes/code/{nodeCode}/details` — `nodeCode` is the objective element code (e.g. `AAC.1.a`).
   * @see ComplaincePlatform/docs/backend/gateway-knowledge-base-api-specification.md
   */
  getNodeDetailsByCode(nodeCode: string): Observable<NodeDetailsDto> {
    return this.get<NodeDetailsDto>(
      this.kb(`/nodes/code/${encodeURIComponent(nodeCode)}/details`),
    );
  }

  getKnowledgeByNodeId(nodeId: string): Observable<KnowledgeItemDto[]> {
    return this.get<KnowledgeItemDto[]>(
      this.kb(`/knowledge/node/${encodeURIComponent(nodeId)}`),
    );
  }

  /**
   * `GET …/knowledge/node/code/{nodeCode}` — no `frameworkId` query (spec 2026-04-13).
   */
  getKnowledgeByNodeCode(nodeCode: string): Observable<KnowledgeItemDto[]> {
    return this.get<KnowledgeItemDto[]>(
      this.kb(`/knowledge/node/code/${encodeURIComponent(nodeCode)}`),
    );
  }

  postAiAsk(body: AiAskRequestDto): Observable<AiAskResponseDto> {
    return this.post<AiAskResponseDto>(this.kb('/ai/ask'), body);
  }

  /**
   * Read-only OE binding: prefer `GET …/nodes/code/{nodeCode}/details`, then
   * `GET …/knowledge/node/code/{nodeCode}` when details has no bindable content or returns 404.
   * HTTP **409** (`RESOURCE_CONFLICT`) propagates.
   * @see WebApp `docs/backend/knowledge-base-wizard-ui-binding.md`
   */
  resolveNodeDetailsForObjectiveElement(
    oe: ObjectiveElement,
  ): Observable<NodeDetailsDto | null> {
    const code = oe.code?.trim();
    if (!code) return of(null);

    const listFallback$ = this.getKnowledgeByNodeCode(code).pipe(
      map((items) => syntheticNodeDetailsFromKnowledgeItems(items, oe)),
      map((syn) => (syn && hasBindableNodeDetailsContent(syn) ? syn : null)),
      catchError((e2: unknown) => {
        const h2 = e2 as HttpErrorResponse;
        if (h2?.status === 409) {
          return throwError(() => e2);
        }
        return of(null);
      }),
    );

    return this.getNodeDetailsByCode(code).pipe(
      switchMap((dto) => {
        if (hasBindableNodeDetailsContent(dto)) {
          return of(dto);
        }
        return listFallback$;
      }),
      catchError((err: unknown) => {
        const http = err as HttpErrorResponse;
        if (http?.status === 409) {
          return throwError(() => err);
        }
        if (http?.status === 404) {
          return listFallback$;
        }
        return throwError(() => err);
      }),
    );
  }
}
