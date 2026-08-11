import {

  Component,

  ChangeDetectionStrategy,

  signal,

  inject,

  DestroyRef,

  OnInit,

  computed,

  OnDestroy,

} from '@angular/core';

import { CommonModule } from '@angular/common';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { timer, Subscription } from 'rxjs';

import { switchMap } from 'rxjs/operators';

import { ComplianceMaturityService } from '../services/compliance-maturity.service';

import { ComplianceMaturityDashboardModel } from '../models/compliance-maturity.model';

import { CmiHeaderComponent } from '../components/cmi-header/cmi-header.component';

import { CmExecutiveKpiComponent } from '../components/cm-executive-kpi/cm-executive-kpi.component';

import { CmJourneyTimelineComponent } from '../components/cm-journey-timeline/cm-journey-timeline.component';

import { CmJourneySidebarComponent } from '../components/cm-journey-sidebar/cm-journey-sidebar.component';

import { CoreComplianceComponent } from '../components/core-compliance/core-compliance.component';

import { PredictedOpportunitiesComponent } from '../components/predicted-opportunities/predicted-opportunities.component';

import { JourneyActionsComponent } from '../components/journey-actions/journey-actions.component';

import { NextActionsComponent } from '../components/next-actions/next-actions.component';

import { CmGovernanceForecastComponent } from '../components/cm-governance-forecast/cm-governance-forecast.component';

import { CmStageBlockingComponent } from '../components/cm-stage-blocking/cm-stage-blocking.component';

import { CmUnlockAuditTimelineComponent } from '../components/cm-unlock-audit-timeline/cm-unlock-audit-timeline.component';

import {

  ComplianceHeatmapComponent,

  HeatmapItem,

} from '../../dashboard/components/compliance-heatmap.component';

import { buildChapterHeatmapItems } from '../utils/chapter-heatmap.util';

/** Background CMI refresh interval — 30 s keeps the dashboard reasonably fresh post-OE-completion. */
const CMI_POLL_INTERVAL_MS = 30_000;

@Component({

  selector: 'app-compliance-maturity',

  standalone: true,

  imports: [

    CommonModule,

    CmiHeaderComponent,

    CmExecutiveKpiComponent,

    CmJourneyTimelineComponent,

    CmJourneySidebarComponent,

    CoreComplianceComponent,

    PredictedOpportunitiesComponent,

    JourneyActionsComponent,

    NextActionsComponent,

    CmGovernanceForecastComponent,

    CmStageBlockingComponent,

    CmUnlockAuditTimelineComponent,

    ComplianceHeatmapComponent,

  ],

  changeDetection: ChangeDetectionStrategy.OnPush,

  template: `

    <div class="cm-container">



      @if (isLoading() && !dashboard()) {

        <div class="cm-skeleton" aria-busy="true" aria-label="Loading compliance maturity dashboard">

          <div class="cm-skeleton__header">

            <div class="cm-skeleton__line cm-skeleton__line--title"></div>

            <div class="cm-skeleton__line cm-skeleton__line--subtitle"></div>

          </div>

          <div class="cm-skeleton__kpis">

            @for (i of [1,2,3,4]; track i) {

              <div class="cm-skeleton__kpi"></div>

            }

          </div>

          <div class="cm-skeleton__timeline"></div>

          <div class="cm-skeleton__layout">

            <div class="cm-skeleton__main"></div>

            <div class="cm-skeleton__sidebar"></div>

          </div>

        </div>

      }



      @if (error() && !isLoading()) {

        <div class="cm-error" role="alert">

          <div class="cm-error__icon" aria-hidden="true">

            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">

              <circle cx="12" cy="12" r="10"/>

              <path d="M12 8v4m0 4h.01" stroke-linecap="round"/>

            </svg>

          </div>

          <h2 class="cm-error__title">Unable to load Compliance Maturity</h2>

          <p class="cm-error__message">{{ error() }}</p>

          <button type="button" class="cm-error__retry" (click)="loadDashboard()">Try again</button>

        </div>

      }



      @if (dashboard(); as data) {

        <div class="cm-content">

          <app-cmi-header [dashboard]="data" />



          <app-cm-executive-kpi [dashboard]="data" />



          <app-cm-journey-timeline

            [stages]="data.stages"

            [currentStage]="data.currentStage"

          />



          @if (data.accreditationForecast) {

            <app-cm-governance-forecast [forecast]="data.accreditationForecast" />

          }



          <app-cm-stage-blocking [blocking]="data.activeStageBlocking ?? null" />



          <div class="cm-layout">

            <div class="cm-main">

              <app-core-compliance [summary]="data.coreSummary" />



              <app-cm-unlock-audit-timeline [entries]="data.unlockAuditTimeline ?? []" />



              <div class="cm-predicted-stack">

                <app-predicted-opportunities

                  title="Predicted Gaps — Commitment"

                  subtitle="Advisory future-stage gaps (not deficiencies)"

                  [summary]="data.commitmentSummary"

                />

                <app-predicted-opportunities

                  title="Predicted Gaps — Achievement"

                  subtitle="Advisory quality and performance readiness challenges"

                  [summary]="data.achievementSummary"

                />

                <app-predicted-opportunities

                  title="Predicted Gaps — Excellence"

                  subtitle="Advisory excellence and innovation opportunities"

                  [summary]="data.excellenceSummary"

                />

              </div>



              <app-compliance-heatmap

                title="Compliance Heatmap by Chapter"

                [data]="heatmapItems()"

              />

            </div>



            <div class="cm-sidebar">

              <app-cm-journey-sidebar [dashboard]="data" />

              <app-next-actions [actions]="data.nextActions" />

              <app-journey-actions [dashboard]="data" />

            </div>

          </div>

        </div>

      }



    </div>

  `,

  styles: [`

    :host {

      display: block;

      --cm-gap: 16px;

    }



    .cm-container {


      margin: 0 auto;

      padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);

    }



    .cm-content {

      display: flex;

      flex-direction: column;

      gap: var(--cm-gap);

    }



    .cm-layout {

      display: grid;

      grid-template-columns: minmax(0, 7fr) minmax(280px, 3fr);

      gap: var(--cm-gap);

      align-items: start;

    }



    .cm-main {

      display: flex;

      flex-direction: column;

      gap: var(--cm-gap);

      min-width: 0;

    }



    .cm-predicted-stack {

      display: flex;

      flex-direction: column;

      gap: var(--cm-gap);

    }



    .cm-sidebar {

      display: flex;

      flex-direction: column;

      gap: var(--cm-gap);

      position: sticky;

      top: 16px;

    }



    /* ── Skeleton ── */

    .cm-skeleton {

      display: flex;

      flex-direction: column;

      gap: var(--cm-gap);

      animation: cm-pulse 1.4s ease-in-out infinite;

    }



    @keyframes cm-pulse {

      0%, 100% { opacity: 1; }

      50% { opacity: 0.55; }

    }



    .cm-skeleton__header,

    .cm-skeleton__timeline,

    .cm-skeleton__main,

    .cm-skeleton__sidebar,

    .cm-skeleton__kpi {

      background: var(--card-bg);

      border: 1px solid var(--border-color);

      border-radius: 12px;

    }



    .cm-skeleton__header { padding: 24px; height: 120px; }

    .cm-skeleton__timeline { height: 140px; }

    .cm-skeleton__main { height: 480px; }

    .cm-skeleton__sidebar { height: 480px; }



    .cm-skeleton__kpis {

      display: grid;

      grid-template-columns: repeat(4, 1fr);

      gap: var(--cm-gap);

    }



    .cm-skeleton__kpi { height: 96px; }



    .cm-skeleton__layout {

      display: grid;

      grid-template-columns: 7fr 3fr;

      gap: var(--cm-gap);

    }



    .cm-skeleton__line {

      background: var(--bg-tertiary);

      border-radius: 4px;

      margin-bottom: 8px;

    }



    .cm-skeleton__line--title { height: 28px; width: 40%; }

    .cm-skeleton__line--subtitle { height: 16px; width: 60%; }



    /* ── Error ── */

    .cm-error {

      display: flex;

      flex-direction: column;

      align-items: center;

      gap: 12px;

      padding: 64px 24px;

      text-align: center;

    }



    .cm-error__icon { color: var(--danger-color); opacity: 0.75; }



    .cm-error__title {

      font-size: 20px;

      font-weight: 600;

      color: var(--text-primary);

      margin: 0;

    }



    .cm-error__message {

      font-size: 15px;

      color: var(--text-secondary);

      margin: 0;

      max-width: 480px;

    }



    .cm-error__retry {

      font-size: 14px;

      font-weight: 600;

      color: var(--primary-color);

      background: var(--primary-bg-light);

      border: 1px solid transparent;

      border-radius: 8px;

      padding: 8px 20px;

      cursor: pointer;

      transition: background 0.15s;

    }



    .cm-error__retry:hover {

      background: var(--hover-bg);

    }



    @media (max-width: 1280px) {

      .cm-layout,

      .cm-skeleton__layout {

        grid-template-columns: 1fr;

      }



      .cm-sidebar {

        position: static;

      }



      .cm-skeleton__kpis {

        grid-template-columns: repeat(2, 1fr);

      }

    }



    @media (max-width: 768px) {

      .cm-container {

        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);

      }



      .cm-skeleton__kpis {

        grid-template-columns: 1fr;

      }

    }

  `],

})

export class ComplianceMaturityComponent implements OnInit, OnDestroy {

  private readonly service = inject(ComplianceMaturityService);

  private readonly destroyRef = inject(DestroyRef);



  dashboard = signal<ComplianceMaturityDashboardModel | null>(null);

  isLoading = signal(true);

  error = signal<string | null>(null);

  private pollSub: Subscription | null = null;



  heatmapItems = computed<HeatmapItem[]>(() => {

    const data = this.dashboard();

    return data ? buildChapterHeatmapItems(data) : [];

  });



  ngOnInit(): void {

    this.loadDashboard();

    this.startPolling();

  }

  ngOnDestroy(): void {

    this.pollSub?.unsubscribe();

  }

  private startPolling(): void {

    // Poll after the initial load so the first request doesn't double up.
    this.pollSub = timer(CMI_POLL_INTERVAL_MS, CMI_POLL_INTERVAL_MS)

      .pipe(

        switchMap(() => this.service.getComplianceMaturityDashboard()),

        takeUntilDestroyed(this.destroyRef),

      )

      .subscribe({

        next: (data) => this.dashboard.set(data),

        // Polling errors are silent — do not overwrite the existing dashboard with an error banner.
        error: () => { /* ignore */ },

      });

  }



  loadDashboard(): void {

    this.isLoading.set(true);

    this.error.set(null);



    this.service

      .getComplianceMaturityDashboard()

      .pipe(takeUntilDestroyed(this.destroyRef))

      .subscribe({

        next: (data) => {

          this.dashboard.set(data);

          this.isLoading.set(false);

        },

        error: (err) => {

          this.error.set(

            err?.error?.message ??

            err?.error?.statusText ??

            err?.message ??

            'An unexpected error occurred. Please try again.',

          );

          this.isLoading.set(false);

        },

      });

  }

}


