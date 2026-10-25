import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import {
  RoleExperienceService,
  type RoleExperienceHome,
} from '../../../core/services/role-experience.service';
import { WorkspaceSectionComponent } from './workspace-layout';
import { WorkspaceSkeletonComponent } from './workspace-states';

/**
 * Role dashboard — Stage 2, Phases 1–5.
 *
 * Answers the three questions a workspace should answer on arrival: what needs
 * my attention, what should I do next, and what is the current state.
 *
 * Composes the existing {@link RoleExperienceService} rather than introducing a
 * parallel personalisation model. That service already resolves a persona, its
 * priorities, attention counts and quick actions from `/role-experience/home`,
 * with per-role local fallbacks when the endpoint is unavailable — so this is
 * presentation over an existing capability, not new behaviour.
 *
 * Platform-level (Phase 14): nothing here is Audit-specific, so Compliance,
 * Risk and Quality can mount the same dashboard and get their own persona's
 * view from the same service.
 *
 * Ordering is deliberate and matches Phase 1: pending work and quick actions
 * come before metrics, because a console should open on what to *do*, not on
 * what to read.
 */
@Component({
  selector: 'app-role-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, WorkspaceSectionComponent, WorkspaceSkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <app-workspace-skeleton variant="cards" [count]="4" label="Loading your workspace" />
    } @else if (home(); as h) {
      <!-- Pending work first: the operational inbox for this role (Phase 5). -->
      <app-workspace-section heading="Needs your attention">
        <ul class="attention">
          @for (item of attentionItems(h); track item.label) {
            <li class="attention__item" [class.is-flagged]="item.count > 0">
              <span class="attention__count">{{ item.count }}</span>
              <span class="attention__label">{{ item.label }}</span>
            </li>
          }
        </ul>
      </app-workspace-section>

      @if (h.quickActions.length) {
        <app-workspace-section heading="Quick actions">
          <div class="actions">
            @for (action of h.quickActions; track action.href) {
              <a class="action" [routerLink]="action.href">{{ action.label }}</a>
            }
          </div>
        </app-workspace-section>
      }

      @if (h.priorities.length) {
        <app-workspace-section heading="Priorities">
          <ul class="priorities">
            @for (p of h.priorities; track p.title) {
              <li class="priority" [class]="'sev-' + (p.severity || '').toLowerCase()">
                @if (p.href) {
                  <a [routerLink]="p.href" class="priority__title">{{ p.title }}</a>
                } @else {
                  <span class="priority__title">{{ p.title }}</span>
                }
                @if (p.rationale) {
                  <span class="priority__why">{{ p.rationale }}</span>
                }
              </li>
            }
          </ul>
        </app-workspace-section>
      }
    }
  `,
  styleUrl: './role-dashboard.component.scss',
})
export class RoleDashboardComponent {
  private readonly roleExperience = inject(RoleExperienceService);

  protected readonly home = signal<RoleExperienceHome | null>(null);
  protected readonly loading = signal(true);

  constructor() {
    this.roleExperience
      .getHome()
      .pipe(
        // The service already falls back to a local pack per role, so a failure
        // here means no persona at all — render nothing rather than an error,
        // since the dashboard is a convenience over the workspace beneath it.
        catchError(() => of(null)),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((h) => this.home.set(h));
  }

  /** Attention counts flattened into a display list, in escalation order. */
  protected attentionItems(h: RoleExperienceHome): { label: string; count: number }[] {
    const a = h.attentionCounts;
    return [
      { label: 'Overdue tasks', count: a.overdueTasks },
      { label: 'Pending reviews', count: a.pendingReviews },
      { label: 'Critical CAPAs', count: a.criticalCapas },
      { label: 'Upcoming audits', count: a.upcomingAudits },
    ];
  }
}
