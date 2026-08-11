import { Injectable, inject } from '@angular/core';
import { catchError, of, tap } from 'rxjs';
import { OrganizationBrandingApiService } from '../../features/settings/services/organization-branding-api.service';
import {
  BrandingColorsModel,
  OrganizationBrandingModel,
} from '../../features/settings/models/organization-branding.model';

/**
 * Applies organization branding as runtime CSS variables on documentElement.
 * Best-effort: failures are ignored so shell still loads.
 *
 * UI-5 — **Identity only.** Branding colours drive the `--color-identity-*`
 * tokens, which are consumed by customer-owned identity surfaces (the header
 * organization mark, logo chrome). They must NOT touch `--color-primary`,
 * `--primary-color`, `--mat-sys-primary`, or any other interaction accent:
 * inline styles set here outrank every stylesheet, so writing those would let
 * an organization's brand colour repaint every button, focus ring, tab, and
 * chip in the product — which is exactly the drift this separation prevents.
 * The interaction accent is always AOVIAS Primary Blue.
 */
@Injectable({ providedIn: 'root' })
export class OrganizationBrandingThemeService {
  private readonly api = inject(OrganizationBrandingApiService);
  private loaded = false;

  loadAndApply(): void {
    if (this.loaded) return;
    this.api
      .getBranding()
      .pipe(
        tap((branding) => {
          this.applyColors(branding.config.colors);
          this.applyFavicon(branding);
          this.loaded = true;
        }),
        catchError(() => of(null)),
      )
      .subscribe();
  }

  applyColors(colors: BrandingColorsModel | null | undefined): void {
    if (!colors) return;
    const root = document.documentElement;

    if (colors.primary) {
      const primary = colors.primary;
      root.style.setProperty('--color-identity-primary', primary);
      root.style.setProperty(
        '--color-identity-primary-soft',
        `color-mix(in srgb, ${primary} 12%, transparent)`,
      );
      root.style.setProperty(
        '--color-identity-primary-border',
        `color-mix(in srgb, ${primary} 35%, transparent)`,
      );
    }
    if (colors.secondary) {
      root.style.setProperty('--color-identity-secondary', colors.secondary);
    }
    if (colors.accent) {
      root.style.setProperty('--color-identity-accent', colors.accent);
    }
  }

  /**
   * Clears any identity overrides applied by a previous organization. Call on
   * organization switch so a tenant's colours never leak into the next session.
   */
  resetColors(): void {
    const root = document.documentElement;
    for (const token of [
      '--color-identity-primary',
      '--color-identity-primary-soft',
      '--color-identity-primary-border',
      '--color-identity-secondary',
      '--color-identity-accent',
    ]) {
      root.style.removeProperty(token);
    }
    this.loaded = false;
  }

  private applyFavicon(branding: OrganizationBrandingModel): void {
    const url = branding.assets?.faviconUrl;
    if (!url || typeof document === 'undefined') return;
    let link = document.querySelector<HTMLLinkElement>("link[rel='icon']");
    if (!link) {
      link = document.createElement('link');
      link.rel = 'icon';
      document.head.appendChild(link);
    }
    link.href = url;
  }
}
