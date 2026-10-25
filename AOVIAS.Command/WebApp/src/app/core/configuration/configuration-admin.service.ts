import { Injectable, computed, inject, signal } from '@angular/core';
import { catchError, of, tap } from 'rxjs';
import {
  ConfigurationApiService,
  type ConfigurationOverrideRequest,
} from './configuration-api.service';
import type { ConfigurationValueDescriptor } from './configuration-catalog';
import { PLATFORM_CONFIGURATION_CATALOGS, catalogFor } from './platform-configuration.catalogs';
import { PlatformConfigurationService } from './platform-configuration.service';

/** A category as the Configuration Center lists it. */
export interface ConfigurationCategorySummary {
  readonly code: string;
  readonly description: string;
  /** How many values are currently resolved for this organization. */
  readonly valueCount: number;
  /** True when this organization has overridden anything in the category. */
  readonly isOverridden: boolean;
}

/**
 * The write side of platform configuration, for the Configuration Center.
 *
 * <p>
 * **Why this exists separately from {@link PlatformConfigurationService}.** That service is injected
 * by roughly thirty consumer screens and is deliberately read-only from their point of view — a
 * grid should not be able to write configuration by accident. Administration needs the writes, and
 * only Administration, so they live behind their own service rather than widening the surface every
 * consumer sees.
 * </p>
 *
 * <p>
 * **Saves propagate without a reload.** The write endpoints return the whole re-resolved category,
 * which is handed straight to the store. Every screen bound to that category updates in place — the
 * runtime propagation the platform already had for loads, now for edits too.
 * </p>
 */
@Injectable({ providedIn: 'root' })
export class ConfigurationAdminService {
  private readonly api = inject(ConfigurationApiService);
  private readonly configuration = inject(PlatformConfigurationService);

  private readonly savingCode = signal<string | null>(null);
  private readonly errorMessage = signal<string | null>(null);

  /** Value code currently being written, so the editor can disable just that row. */
  readonly saving = this.savingCode.asReadonly();
  readonly error = this.errorMessage.asReadonly();

  /**
   * Every category an administrator may manage, in registry order.
   *
   * Derived from the same registry the store seeds from, so a domain added to a catalog module
   * appears here with no further wiring — which is what keeps this one editor rather than one page
   * per domain.
   */
  readonly categories = computed<readonly ConfigurationCategorySummary[]>(() =>
    PLATFORM_CONFIGURATION_CATALOGS.map((catalog) => ({
      code: catalog.code,
      description: catalog.description,
      valueCount: this.configuration.values(catalog.code)().length,
      isOverridden: this.configuration.isCategoryConfigured(catalog.code),
    })),
  );

  /** Values of one category, as currently resolved. */
  valuesOf(code: string) {
    return this.configuration.values(code);
  }

  /** The platform-default values of a category, for showing what an override departs from. */
  seedOf(code: string): readonly ConfigurationValueDescriptor[] {
    return catalogFor(code)?.seed ?? [];
  }

  /**
   * Save an administrator's edit to one value.
   *
   * <p>
   * Sends only what changed. The server keeps any field the request omits, so an edit that renames
   * a value cannot silently rewrite its ordering or its attributes — which matters most for the
   * workflow categories, where the attributes are the rules.
   * </p>
   */
  save(categoryCode: string, request: ConfigurationOverrideRequest): void {
    this.savingCode.set(request.code);
    this.errorMessage.set(null);

    this.api
      .saveOverride(categoryCode, request)
      .pipe(
        tap((category) => this.configuration.applyCategory(category)),
        catchError(() => {
          // Reads fall back to the seed and stay usable; a failed *write* must reach the
          // administrator, because silently discarding an edit is worse than showing an error.
          this.errorMessage.set('Could not save this change. Nothing was applied.');
          return of(null);
        }),
        tap(() => this.savingCode.set(null)),
      )
      .subscribe();
  }

  /** Withdraw this organization's override of a value, restoring the platform default. */
  restore(categoryCode: string, valueCode: string): void {
    this.savingCode.set(valueCode);
    this.errorMessage.set(null);

    this.api
      .restoreDefault(categoryCode, valueCode)
      .pipe(
        tap((category) => this.configuration.applyCategory(category)),
        catchError(() => {
          this.errorMessage.set('Could not restore the default. Nothing was changed.');
          return of(null);
        }),
        tap(() => this.savingCode.set(null)),
      )
      .subscribe();
  }

  clearError(): void {
    this.errorMessage.set(null);
  }
}
