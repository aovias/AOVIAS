import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TrialExperienceService } from '../../core/services/trial-experience.service';

@Component({
  selector: 'app-trial-experience-banner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      class="trial-banner"
      *ngIf="trial.loaded() && trial.status() as st"
      [class.grace]="trial.isGrace()"
      [class.readonly]="trial.isReadOnly()"
      [class.warn]="!trial.isReadOnly() && !trial.isGrace() && (trial.daysRemaining() ?? 99) <= 7"
    >
      <ng-container *ngIf="trial.isReadOnly(); else activeTpl">
        <strong>Read-only mode</strong>
        <span>Your trial has expired. Existing data remains viewable; new records, users, and seat changes are blocked.</span>
        <a class="cta" [href]="upgradeUrl" target="_blank" rel="noopener">Upgrade options</a>
      </ng-container>
      <ng-template #activeTpl>
        <ng-container *ngIf="trial.isGrace(); else daysTpl">
          <strong>Grace period</strong>
          <span>Trial grace is active — upgrade soon to avoid read-only mode. Status: {{ st.status }}</span>
          <a class="cta" [href]="upgradeUrl" target="_blank" rel="noopener">Upgrade</a>
        </ng-container>
        <ng-template #daysTpl>
          <ng-container *ngIf="(trial.daysRemaining() ?? 999) <= 7">
            <strong>Trial</strong>
            <span>{{ trial.daysRemaining() }} day(s) remaining · {{ st.usedSeats }}/{{ st.maxSeats }} seats</span>
            <a class="cta" [href]="upgradeUrl" target="_blank" rel="noopener">Upgrade</a>
          </ng-container>
        </ng-template>
      </ng-template>
    </div>
  `,
  styles: [
    `
      .trial-banner {
        display: flex;
        flex-wrap: wrap;
        gap: 10px 16px;
        align-items: center;
        padding: 8px 16px;
        font-size: 13px;
        background: #eff6ff;
        border-bottom: 1px solid #bfdbfe;
        color: #1e3a8a;
      }
      .trial-banner.warn {
        background: #fffbeb;
        border-color: #fcd34d;
        color: #92400e;
      }
      .trial-banner.grace {
        background: #fff7ed;
        border-color: #fdba74;
        color: #9a3412;
      }
      .trial-banner.readonly {
        background: #fef2f2;
        border-color: #fecaca;
        color: #991b1b;
      }
      .cta {
        margin-left: auto;
        font-weight: 600;
        color: inherit;
      }
    `,
  ],
})
export class TrialExperienceBannerComponent implements OnInit {
  readonly trial = inject(TrialExperienceService);
  readonly upgradeUrl = '/docs/upgrade';

  ngOnInit() {
    this.trial.refresh().subscribe();
  }
}
