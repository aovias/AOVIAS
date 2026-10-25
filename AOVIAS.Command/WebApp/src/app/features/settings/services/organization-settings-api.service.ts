import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import {
  OrganizationSettingsModel,
  UpdateOrganizationSettingsPayload,
} from '../models/organization-settings.model';

@Injectable({ providedIn: 'root' })
export class OrganizationSettingsApiService extends BaseApiService {
  private readonly base = '/settings/organization';

  getSettings(): Observable<OrganizationSettingsModel> {
    return this.get<OrganizationSettingsModel>(this.base);
  }

  updateSettings(
    payload: UpdateOrganizationSettingsPayload,
  ): Observable<OrganizationSettingsModel> {
    return this.put<OrganizationSettingsModel>(this.base, payload);
  }

  uploadLogo(file: File): Observable<OrganizationSettingsModel> {
    const formData = new FormData();
    formData.append('file', file);
    return this.post<OrganizationSettingsModel>(`${this.base}/logo`, formData);
  }

  removeLogo(): Observable<OrganizationSettingsModel> {
    return this.delete<OrganizationSettingsModel>(`${this.base}/logo`);
  }

  logoPreviewUrl(cacheBust?: number): string {
    const suffix = cacheBust ? `?v=${cacheBust}` : '';
    return `${this.apiBaseUrl}${this.base}/logo${suffix}`;
  }
}
