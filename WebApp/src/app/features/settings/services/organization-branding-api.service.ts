import { Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import {
  BrandingAssetKey,
  BrandingConfigModel,
  OrganizationBrandingModel,
} from '../models/organization-branding.model';

@Injectable({ providedIn: 'root' })
export class OrganizationBrandingApiService extends BaseApiService {
  private readonly brandingBase = '/settings/organization/branding';
  private readonly orgBase = '/settings/organization';

  getBranding(): Observable<OrganizationBrandingModel> {
    return this.get<OrganizationBrandingModel>(this.brandingBase);
  }

  updateBranding(
    config: BrandingConfigModel,
    rowVersion: string | null,
  ): Observable<OrganizationBrandingModel> {
    return this.put<OrganizationBrandingModel>(this.brandingBase, { config, rowVersion });
  }

  uploadPrimaryLogo(file: File): Observable<OrganizationBrandingModel> {
    const formData = new FormData();
    formData.append('file', file);
    return this.post<unknown>(`${this.orgBase}/logo`, formData).pipe(
      switchMap(() => this.getBranding()),
    );
  }

  removePrimaryLogo(): Observable<OrganizationBrandingModel> {
    return this.delete<unknown>(`${this.orgBase}/logo`).pipe(switchMap(() => this.getBranding()));
  }

  uploadAsset(assetKey: BrandingAssetKey, file: File): Observable<OrganizationBrandingModel> {
    const formData = new FormData();
    formData.append('file', file);
    return this.post<OrganizationBrandingModel>(
      `${this.brandingBase}/assets/${assetKey}`,
      formData,
    );
  }

  removeAsset(assetKey: BrandingAssetKey): Observable<OrganizationBrandingModel> {
    return this.delete<OrganizationBrandingModel>(`${this.brandingBase}/assets/${assetKey}`);
  }

  assetPreviewUrl(assetKey: BrandingAssetKey | 'primary', cacheBust?: number): string {
    const suffix = cacheBust ? `?v=${cacheBust}` : '';
    if (assetKey === 'primary') return `${this.apiBaseUrl}${this.orgBase}/logo${suffix}`;
    return `${this.apiBaseUrl}${this.brandingBase}/assets/${assetKey}${suffix}`;
  }
}
