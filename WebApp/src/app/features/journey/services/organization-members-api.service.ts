import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { LoggerService } from '../../../core/services/logger.service';
import { OrganizationMember } from '../../../core/services/organization.service';
import {
  mapOrganizationMemberApiRow,
  OrganizationMemberApiRow,
} from '../../../shared/models/organization-member-api.model';

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * Dedicated client for organization member directory rows (Phase 60).
 * Assign tasks using {@link OrganizationMember#userId} as `assignedToUserId`.
 */
@Injectable({
  providedIn: 'root',
})
export class OrganizationMembersApiService extends BaseApiService {
  constructor(private readonly logger: LoggerService) {
    super();
  }

  /**
   * GET `{apiBaseUrl}/organization-members/organization/{organizationId}`.
   * Response is envelope-unwrapped by {@link apiResponseInterceptor} to `data` (typically an array).
   */
  getMembersForOrganization(organizationId: string): Observable<OrganizationMember[]> {
    const id = organizationId?.trim() ?? '';
    if (!id || !GUID_RE.test(id)) {
      this.logger.warn('Organization members fetch skipped: invalid or missing organization id', {
        hasValue: !!id,
      });
      return of([]);
    }

    return this.get<unknown>(`/organization-members/organization/${id}`).pipe(
      map((body) => this.normalizeMemberArray(body)),
      map((rows) => rows.map((r) => mapOrganizationMemberApiRow(r as OrganizationMemberApiRow))),
    );
  }

  private normalizeMemberArray(body: unknown): OrganizationMemberApiRow[] {
    if (Array.isArray(body)) {
      return body as OrganizationMemberApiRow[];
    }
    if (body && typeof body === 'object') {
      const inner = (body as { items?: unknown; data?: unknown }).items ?? (body as { data?: unknown }).data;
      if (Array.isArray(inner)) {
        return inner as OrganizationMemberApiRow[];
      }
    }
    this.logger.warn('Organization members response was not an array; using empty list', {
      type: typeof body,
    });
    return [];
  }
}
