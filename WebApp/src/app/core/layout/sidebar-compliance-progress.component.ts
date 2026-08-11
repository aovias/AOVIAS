import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { combineLatest, debounceTime, distinctUntilChanged, skip } from 'rxjs';
import { ComplianceContextService } from '../services/compliance-context.service';
import { ProgramOverviewSnapshotService } from '../services/program-overview-snapshot.service';
import { ShellOperationalModeService } from './shell-operational-mode.service';
import { environment } from '../../../environments/environment';
import type {
  DashboardSnapshot,
  SnapshotQueryParams,
} from '../../features/dashboard/models/dashboard-snapshot.model';

/**
 * Compliance circular progress — shown in the shell header workspace context.
 */
@Component({
  selector: 'app-sidebar-compliance-progress',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="sidebar-compliance" role="status" aria-live="polite">
      <div class="sidebar-compliance__ring-wrap" [attr.aria-label]="complianceAriaLabel()">
        @if (complianceLoading()) {
          <span class="sidebar-compliance__loading" aria-hidden="true"></span>
        } @else {
          <svg
            class="sidebar-compliance__svg"
            viewBox="0 0 44 44"
            aria-hidden="true"
            focusable="false"
          >
            <circle
              class="sidebar-compliance__track"
              cx="22"
              cy="22"
              [attr.r]="complianceRingRadius"
              fill="none"
            />
            <circle
              class="sidebar-compliance__arc"
              cx="22"
              cy="22"
              [attr.r]="complianceRingRadius"
              fill="none"
              transform="rotate(-90 22 22)"
              [attr.stroke-dasharray]="complianceRingDashArray()"
              [attr.stroke-dashoffset]="complianceRingDashOffset()"
              [attr.data-score]="complianceScoreLevel() ?? 'none'"
            />
          </svg>
          <span class="sidebar-compliance__pct">{{ compliancePercentLabel() }}</span>
        }
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        flex-shrink: 0;
      }

      :host-context(.app-header) .sidebar-compliance__ring-wrap {
        width: 40px;
        height: 40px;
      }

      :host-context(.app-header) .sidebar-compliance__svg {
        width: 40px;
        height: 40px;
      }

      :host-context(.app-header) .sidebar-compliance__pct {
        font-size: 10px;
      }

      .sidebar-compliance {
        display: flex;
        align-items: center;
        justify-content: center;
        min-width: 0;
        padding: 0;
        box-sizing: border-box;
      }

      :host-context(.sidebar-host--collapsed) .sidebar-compliance {
        align-items: center;
        justify-content: center;
      }

      :host-context(.sidebar-host--collapsed) .sidebar-compliance__ring-wrap {
        width: 40px;
        height: 40px;
      }

      :host-context(.sidebar-host--collapsed) .sidebar-compliance__svg {
        width: 40px;
        height: 40px;
      }

      :host-context(.sidebar-host--collapsed) .sidebar-compliance__pct {
        font-size: 10px;
      }

      .sidebar-compliance__ring-wrap {
        position: relative;
        width: 48px;
        height: 48px;
        flex-shrink: 0;
      }

      .sidebar-compliance__svg {
        display: block;
        width: 48px;
        height: 48px;
      }

      .sidebar-compliance__track {
        stroke: var(--border-color);
        stroke-width: 3;
      }

      .sidebar-compliance__arc {
        stroke-width: 3;
        stroke-linecap: round;
        transition: stroke-dashoffset 0.45s ease;
      }

      .sidebar-compliance__arc[data-score='high'] {
        stroke: var(--success-color);
      }

      .sidebar-compliance__arc[data-score='medium'] {
        stroke: var(--warning-color);
      }

      .sidebar-compliance__arc[data-score='low'] {
        stroke: var(--danger-color);
      }

      .sidebar-compliance__arc[data-score='none'] {
        stroke: var(--text-muted);
      }

      .sidebar-compliance__loading {
        position: absolute;
        top: 50%;
        left: 50%;
        width: 28px;
        height: 28px;
        margin: -14px 0 0 -14px;
        border: 2px solid color-mix(in srgb, var(--text-secondary) 22%, transparent);
        border-top-color: var(--primary-color);
        border-radius: 50%;
        animation: sidebarComplianceSpin 0.8s linear infinite;
      }

      @keyframes sidebarComplianceSpin {
        to {
          transform: rotate(360deg);
        }
      }

      .sidebar-compliance__pct {
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 11px;
        font-weight: 700;
        color: var(--text-primary);
        pointer-events: none;
      }
    `,
  ],
})
export class SidebarComplianceProgressComponent implements OnInit {
  private readonly complianceSnapshot = signal<DashboardSnapshot | null>(null);
  readonly complianceLoading = signal(true);
  private readonly complianceRingC = 2 * Math.PI * 16;
  readonly complianceRingRadius = 16;

  readonly opMode = inject(ShellOperationalModeService);
  private readonly programOverviewSnapshot = inject(ProgramOverviewSnapshotService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly destroyRef = inject(DestroyRef);

  readonly compliancePercent = computed((): number | null => {
    const snap = this.complianceSnapshot();
    if (!snap) return null;
    if (snap.isSnapshotAvailable === false) return null;
    if (typeof snap.overallScore === 'number' && !Number.isNaN(snap.overallScore)) {
      return Math.max(0, Math.min(100, Math.round(snap.overallScore)));
    }
    const avg = snap.summary?.avgComplianceScore;
    if (typeof avg === 'number' && !Number.isNaN(avg)) {
      return Math.max(0, Math.min(100, Math.round(avg)));
    }
    return null;
  });

  readonly complianceScoreLevel = computed((): 'high' | 'medium' | 'low' | null => {
    const p = this.compliancePercent();
    if (p == null) return null;
    if (p >= 75) return 'high';
    if (p >= 60) return 'medium';
    return 'low';
  });

  readonly complianceRingDashArray = computed(
    () => `${this.complianceRingC} ${this.complianceRingC}`,
  );

  readonly complianceRingDashOffset = computed(() => {
    const p = this.compliancePercent();
    if (p == null) {
      return this.complianceRingC;
    }
    return this.complianceRingC * (1 - p / 100);
  });

  readonly compliancePercentLabel = computed(() => {
    if (this.complianceLoading()) return '';
    const p = this.compliancePercent();
    if (p == null) return '—';
    return `${p}%`;
  });

  readonly complianceAriaLabel = computed(() => {
    if (this.complianceLoading()) {
      return 'Loading compliance progress';
    }
    const p = this.compliancePercent();
    const mode = this.opMode.label(this.opMode.mode());
    if (p == null) {
      return `Compliance progress unavailable, ${mode}`;
    }
    return `Compliance progress ${p} percent, ${mode}`;
  });

  ngOnInit(): void {
    this.loadComplianceSnapshot();
    combineLatest([this.complianceContext.hospitalId$, this.complianceContext.frameworkId$])
      .pipe(
        skip(1),
        debounceTime(100),
        distinctUntilChanged((prev, curr) => prev[0] === curr[0] && prev[1] === curr[1]),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.loadComplianceSnapshot());
  }

  private getSnapshotParams(): SnapshotQueryParams {
    return {
      hospitalId: this.complianceContext.getHospitalId(),
      frameworkId: this.complianceContext.getFrameworkId(),
    };
  }

  private loadComplianceSnapshot(): void {
    const params = this.getSnapshotParams();
    const cached = this.programOverviewSnapshot.getCached(params);
    if (cached) {
      this.complianceSnapshot.set(cached);
      this.complianceLoading.set(false);
      if (!this.programOverviewSnapshot.isFresh(params, environment.dashboardSnapshotCacheTtlMs)) {
        this.fetchSnapshot(params, true);
      }
      return;
    }

    this.complianceLoading.set(true);
    this.fetchSnapshot(params, false);
  }

  private fetchSnapshot(
    params: ReturnType<SidebarComplianceProgressComponent['getSnapshotParams']>,
    background: boolean,
  ): void {
    this.programOverviewSnapshot.getProgramOverviewSnapshot(params, { background }).subscribe({
      next: (data) => {
        this.complianceSnapshot.set(data);
        this.complianceLoading.set(false);
      },
      error: () => {
        if (!background) {
          this.complianceSnapshot.set(null);
        }
        this.complianceLoading.set(false);
      },
    });
  }
}
