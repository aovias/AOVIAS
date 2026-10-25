import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { combineLatest, of } from 'rxjs';
import { catchError, filter, switchMap, tap } from 'rxjs/operators';
import { JourneyWorkspaceStore } from '../../state/journey-workspace.store';
import { ComplianceContextService } from '../../../../core/services/compliance-context.service';
import { DeficiencyDataService } from '../../services/deficiency-data.service';
import { TaskApiService } from '../../../tasks/task-api.service';
import type { DeficiencyDto } from '../../models/deficiency-api.model';
import type { TaskListItem } from '../../../tasks/models';

@Component({
  selector: 'app-oe-remediation-context',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="oe-rem" aria-labelledby="oe-rem-h">
      <h2 id="oe-rem-h" class="oe-rem__h">Remediation</h2>

      @if (loading()) {
        <p class="oe-rem__muted">Loading deficiencies and tasks…</p>
      } @else {
        <div class="oe-rem__block">
          <h3 class="oe-rem__sub">Open deficiencies</h3>
          @if (openDeficiencies().length === 0) {
            <p class="oe-rem__muted">No open deficiencies for this objective element.</p>
          } @else {
            <ul class="oe-rem__list">
              @for (d of openDeficiencies(); track d.id) {
                <li class="oe-rem__item">
                  <span class="oe-rem__sev" [attr.data-sev]="d.severity">{{ d.severity }}</span>
                  <span class="oe-rem__title">{{ d.title }}</span>
                </li>
              }
            </ul>
          }
        </div>

        <div class="oe-rem__block">
          <h3 class="oe-rem__sub">Open tasks</h3>
          @if (openTasks().length === 0) {
            <p class="oe-rem__muted">No open remediation tasks yet.</p>
          } @else {
            <ul class="oe-rem__list">
              @for (t of openTasks(); track t.id) {
                <li class="oe-rem__item">
                  <a class="oe-rem__task-link" [routerLink]="tasksLink()" [queryParams]="taskQuery(t)">{{
                    t.title || t.objectiveElementCode
                  }}</a>
                  <span class="oe-rem__status">{{ t.status }}</span>
                </li>
              }
            </ul>
          }
        </div>

        @if (header(); as h) {
          <a class="oe-rem__cta" [routerLink]="tasksLink()" [queryParams]="{ search: h.oeCode }">
            View all tasks for {{ h.oeCode }}
          </a>
        }
      }
    </section>
  `,
  styles: [
    `
      .oe-rem__h {
        margin: 0 0 0.65rem;
        font-size: 0.78rem;
        font-weight: 700;
        letter-spacing: 0.05em;
        text-transform: uppercase;
        color: var(--color-text-secondary);
      }
      .oe-rem__sub {
        margin: 0 0 0.35rem;
        font-size: 0.72rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--color-text-muted);
      }
      .oe-rem__block + .oe-rem__block {
        margin-top: 0.85rem;
      }
      .oe-rem__list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 0.45rem;
      }
      .oe-rem__item {
        display: flex;
        align-items: flex-start;
        gap: 0.45rem;
        font-size: 0.82rem;
        line-height: 1.35;
      }
      .oe-rem__sev {
        flex: 0 0 auto;
        font-size: 0.65rem;
        font-weight: 700;
        padding: 0.1rem 0.35rem;
        border-radius: 4px;
        background: var(--color-bg-hover);
      }
      .oe-rem__sev[data-sev='Critical'] {
        background: color-mix(in srgb, #dc2626 18%, transparent);
        color: #b91c1c;
      }
      .oe-rem__title {
        flex: 1;
        color: var(--color-text-primary);
      }
      .oe-rem__task-link {
        flex: 1;
        color: var(--color-brand-primary);
        text-decoration: none;
        font-weight: 600;
      }
      .oe-rem__task-link:hover {
        text-decoration: underline;
      }
      .oe-rem__status {
        font-size: 0.68rem;
        color: var(--color-text-muted);
        white-space: nowrap;
      }
      .oe-rem__muted {
        margin: 0;
        font-size: 0.8rem;
        color: var(--color-text-muted);
      }
      .oe-rem__cta {
        display: inline-block;
        margin-top: 0.85rem;
        font-size: 0.78rem;
        font-weight: 600;
        color: var(--color-brand-primary);
        text-decoration: none;
      }
      .oe-rem__cta:hover {
        text-decoration: underline;
      }
    `,
  ],
})
export class OeRemediationContextComponent {
  private readonly store = inject(JourneyWorkspaceStore);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly deficiencyApi = inject(DeficiencyDataService);
  private readonly taskApi = inject(TaskApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly header = computed(() => this.store.oeWizardHeaderVm());
  readonly loading = signal(false);
  readonly openDeficiencies = signal<DeficiencyDto[]>([]);
  readonly openTasks = signal<TaskListItem[]>([]);

  readonly tasksLink = computed(() => ['/app/tasks']);

  private readonly oeContext = computed(() => ({
    oeId: this.store.selectedOeId(),
    header: this.store.oeWizardHeaderVm(),
  }));

  constructor() {
    combineLatest([
      this.complianceContext.hospitalId$,
      this.complianceContext.frameworkId$,
      toObservable(this.oeContext),
    ])
      .pipe(
        filter(([hospitalId, frameworkId, ctx]) => !!hospitalId && !!frameworkId && !!ctx.oeId && !!ctx.header),
        switchMap(([hospitalId, frameworkId, ctx]) => {
          const oeId = ctx.oeId!;
          const h = ctx.header!;
          this.loading.set(true);
          return combineLatest([
            this.deficiencyApi.getDeficiencies(hospitalId!, frameworkId!, 1, 200).pipe(
              catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 200 })),
            ),
            this.taskApi
              .getTasksPaged(frameworkId!, { search: h.oeCode }, 1, 50, 'table')
              .pipe(catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 50 }))),
          ]).pipe(
            tap(([defList, taskPage]) => {
              const openDefs = (defList.items ?? []).filter(
                (d) =>
                  d.objectiveElementId === oeId &&
                  d.status !== 'Closed' &&
                  d.status !== 'Verified',
              );
              const open = (taskPage.items ?? []).filter(
                (t) => t.objectiveElementId === oeId && t.status !== 'Completed',
              );
              this.openDeficiencies.set(openDefs);
              this.openTasks.set(open);
              this.loading.set(false);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  taskQuery(task: TaskListItem): Record<string, string> {
    return { search: task.objectiveElementCode };
  }
}
