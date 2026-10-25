import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError, finalize, of } from 'rxjs';
import {
  WorkspaceHeaderComponent,
  WorkspacePageComponent,
  WorkspaceSectionComponent,
} from '../../../shared/ui/workspace/workspace-layout';
import {
  WorkspaceEmptyStateComponent,
  WorkspaceErrorStateComponent,
  WorkspaceSkeletonComponent,
} from '../../../shared/ui/workspace/workspace-states';
import { injectAuditWorkspace } from '../workspace/services/audit-workspace-participation.service';
import {
  AuditManagementApiService,
  type AuditSimulationResultDto,
} from '../services/audit-management-api.service';

@Component({
  selector: 'app-audit-simulation',
  standalone: true,
  imports: [
    CommonModule,
    WorkspacePageComponent,
    WorkspaceHeaderComponent,
    WorkspaceSectionComponent,
    WorkspaceEmptyStateComponent,
    WorkspaceErrorStateComponent,
    WorkspaceSkeletonComponent,
  ],
  template: `
    <app-workspace-page>
      <app-workspace-header
        title="Audit Simulation"
        description="Runs Compliance AuditSimulationService against live hospital compliance data."
      >
        <button
          headerActions
          type="button"
          class="btn-primary"
          (click)="runSimulation()"
          [disabled]="running()"
        >
          {{ running() ? 'Simulating…' : 'Run simulation' }}
        </button>
      </app-workspace-header>

      @if (error()) {
        <app-workspace-error-state
          [message]="error()!"
          hint="Check that a hospital and framework are selected, then run again."
          (retry)="runSimulation()"
        />
      }

      @if (running()) {
        <!-- Stage 1.3, Step 8: this page previously showed nothing at all
             while a long-running simulation was in flight. -->
        <app-workspace-skeleton variant="cards" [count]="6" label="Running simulation" />
        <app-workspace-skeleton variant="table" [count]="4" label="Preparing results" />
      } @else if (result(); as r) {
        <section class="metrics">
          <div class="metric">
            <div class="value">{{ r.score }}%</div>
            <div class="label">Simulated score</div>
          </div>
          <div class="metric">
            <div class="value">{{ r.compliantClauses }}/{{ r.totalClauses }}</div>
            <div class="label">Compliant clauses</div>
          </div>
          <div class="metric">
            <div class="value">{{ r.majorNCCount }}</div>
            <div class="label">Major NC</div>
          </div>
          <div class="metric">
            <div class="value">{{ r.minorNCCount }}</div>
            <div class="label">Minor NC</div>
          </div>
          <div class="metric">
            <div class="value">{{ r.observationCount }}</div>
            <div class="label">Observations</div>
          </div>
          <div class="metric">
            <div class="value">{{ r.openTasks }}</div>
            <div class="label">Open tasks</div>
          </div>
        </section>

        <app-workspace-section heading="High-risk chapters">
          <table>
            <thead>
              <tr>
                <th>Chapter</th>
                <th>Completion</th>
                <th>OE done</th>
              </tr>
            </thead>
            <tbody>
              @for (c of r.highRiskChapters; track c.chapterId) {
                <tr>
                  <td>{{ c.chapterCode }} — {{ c.chapterName }}</td>
                  <td>{{ c.completionPercentage | number: '1.0-1' }}%</td>
                  <td>{{ c.completedObjectiveElements }}/{{ c.totalObjectiveElements }}</td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="3" class="muted">No high-risk chapters detected.</td>
                </tr>
              }
            </tbody>
          </table>
        </app-workspace-section>
      } @else {
        <app-workspace-empty-state
          icon="◎"
          title="No simulation run yet"
          description="Evaluate accreditation readiness from current evidence, tasks and open risks."
          actionLabel="Run simulation"
          (action)="runSimulation()"
        />
      }
    </app-workspace-page>
  `,
  styles: [
    `
      .muted {
        color: var(--text-secondary);
        font-size: var(--font-size-caption);
        margin: 0;
      }
      .btn-primary {
        padding: 10px 18px;
        border: none;
        border-radius: 6px;
        background: var(--primary-color, var(--color-primary));
        color: #fff;
        cursor: pointer;
      }
      .btn-primary:disabled {
        opacity: 0.6;
        cursor: default;
      }
      .metrics {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
        gap: 12px;
        margin-bottom: 24px;
      }
      .metric {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 16px;
      }
      .value {
        font-size: 24px;
        font-weight: 700;
      }
      .label {
        font-size: 12px;
        color: var(--text-secondary, #666);
        margin-top: 4px;
        text-transform: uppercase;
      }

      table {
        width: 100%;
        border-collapse: collapse;
        font-size: 14px;
      }
      th,
      td {
        text-align: left;
        padding: 10px 8px;
        border-bottom: 1px solid var(--border-color, #f0f0f0);
      }
    `,
  ],
})
export class AuditSimulationComponent implements OnInit {
  private readonly api = inject(AuditManagementApiService);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  readonly running = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<AuditSimulationResultDto | null>(null);

  ngOnInit(): void {
    this.publishParticipation();
  }

  /**
   * Simulation sits in the execution stage: a mock audit is a rehearsal of it.
   * The toolbar carries the run action so it is reachable without hunting for
   * the in-page button.
   */
  private publishParticipation(): void {
    const result = this.result();

    this.workspace.set({
      stage: 'execution',
      subject: { kind: 'audit', title: 'Audit simulation' },
      actions: [
        {
          id: 'simulation.run',
          label: this.running() ? 'Running…' : 'Run simulation',
          variant: 'primary',
          disabled: this.running(),
          disabledReason: 'Simulation already running',
          run: () => this.runSimulation(),
        },
      ],
      workflowState: {
        stage: 'execution',
        statusLabel: result ? 'Simulation complete' : 'Not yet run',
        completionCriteria: 'Run a simulation to score readiness',
      },
      aiContext: {
        facts: result
          ? {
              simulationScore: String(result.score),
              majorNonConformances: String(result.majorNCCount),
              minorNonConformances: String(result.minorNCCount),
              compliantClauses: `${result.compliantClauses} of ${result.totalClauses}`,
            }
          : { simulationScore: 'not run' },
      },
    });
  }

  runSimulation(): void {
    this.running.set(true);
    this.error.set(null);
    this.publishParticipation();
    this.api
      .runSimulation()
      .pipe(
        catchError(() => {
          this.error.set('Simulation failed. Ensure hospital/framework context is selected.');
          return of(null);
        }),
        finalize(() => this.running.set(false)),
      )
      .subscribe((r) => {
        this.result.set(r);
        this.publishParticipation();
      });
  }
}
