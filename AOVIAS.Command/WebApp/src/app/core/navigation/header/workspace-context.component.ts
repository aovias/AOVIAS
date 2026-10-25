import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { OrganizationService } from '../../services/organization.service';
import { ComplianceContextService } from '../../services/compliance-context.service';
import { AppContextService } from '../../services/app-context.service';

/**
 * Header workspace brand — hospital + organization labels only.
 * Organization switching lives in the user profile menu.
 */
@Component({
  selector: 'app-workspace-context',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="workspace-context" [attr.aria-busy]="switching()">
      @if (switching()) {
        <span class="workspace-context__spinner" aria-hidden="true"></span>
      }
      <span class="workspace-context__identity-rail" aria-hidden="true"></span>
      <div class="workspace-context__stack">
        <span class="workspace-context__hospital">{{ hospitalName() }}</span>
        <span class="workspace-context__org">{{ organizationName() }}</span>
      </div>
    </div>
  `,
  styles: `
    :host {
      display: inline-flex;
      min-width: 0;
      max-width: 320px;
    }

    .workspace-context {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      min-height: var(--header-control-height, 36px);
      padding: 0;
      color: var(--color-text-primary, var(--text-primary));
      min-width: 0;
      max-width: 100%;
      text-align: left;
    }

    /*
     * UI-5 — application chrome, so this uses the AOVIAS accent, NOT
     * --color-identity-primary. Binding it to the per-organization brand colour
     * put a non-AOVIAS hue back into the header; branding colour is carried by
     * the logo and server-rendered assets (reports, emails), not by shell chrome.
     */
    .workspace-context__identity-rail {
      flex-shrink: 0;
      inline-size: 3px;
      align-self: stretch;
      min-block-size: 1.75rem;
      border-radius: var(--radius-pill);
      background: var(--color-primary);
    }

    .workspace-context__stack {
      display: flex;
      flex-direction: column;
      min-width: 0;
      gap: 1px;
    }

    .workspace-context__hospital {
      font-size: 12px;
      font-weight: 600;
      line-height: 1.15;
      letter-spacing: 0.02em;
      text-transform: uppercase;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      color: var(--hospital-card-name-color, var(--color-text-primary));
    }

    .workspace-context__org {
      font-size: 11px;
      font-weight: 500;
      color: var(--hospital-card-org-color, var(--color-text-secondary, var(--text-secondary)));
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .workspace-context__spinner {
      width: 14px;
      height: 14px;
      border: 2px solid var(--color-border-subtle, #ccc);
      border-top-color: var(--color-primary, var(--primary-color));
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
      flex-shrink: 0;
    }

    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }

    @media (prefers-reduced-motion: reduce) {
      .workspace-context__spinner {
        animation: none;
      }
    }
  `,
})
export class WorkspaceContextComponent implements OnInit {
  private readonly organizationService = inject(OrganizationService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly appContext = inject(AppContextService);
  private readonly destroyRef = inject(DestroyRef);

  readonly hospitalName = signal('Select Hospital');
  readonly organizationName = signal('Organization');
  readonly switching = signal(false);

  ngOnInit(): void {
    this.appContext.switching$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((v) => this.switching.set(v));

    this.complianceContext.hospitalId$
      .pipe(
        filter((id): id is string => !!id),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((id) => this.updateHospital(id));

    const org = this.organizationService.getOrganization();
    if (org?.organizationName) this.organizationName.set(org.organizationName);

    this.organizationService.organization$
      .pipe(
        filter((o): o is NonNullable<typeof o> => o !== null),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((o) => {
        if (o.organizationName) this.organizationName.set(o.organizationName);
      });

    const current = this.complianceContext.getHospitalId();
    if (current) this.updateHospital(current);
    else {
      const def = this.organizationService.getDefaultHospital();
      if (def) this.hospitalName.set(def.name);
    }
  }

  private updateHospital(hospitalId: string): void {
    const hospital = this.organizationService.getHospitals().find((h) => h.id === hospitalId);
    if (hospital) this.hospitalName.set(hospital.name);
    const org = this.organizationService.getOrganization();
    if (org?.organizationName) this.organizationName.set(org.organizationName);
  }
}
