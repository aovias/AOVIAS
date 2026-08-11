import { ChangeDetectionStrategy, Component, OnDestroy, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { JourneyWorkspaceStore } from '../state/journey-workspace.store';
import { OeExecutionWizardComponent } from '../components/oe-execution-wizard/oe-execution-wizard.component';

/** Dedicated route for the Objective Element compliance wizard (not embedded in Mission Control). */
@Component({
  selector: 'app-objective-element-execution-screen',
  standalone: true,
  imports: [CommonModule, OeExecutionWizardComponent],
  templateUrl: './objective-element-execution-screen.component.html',
  styleUrl: './objective-element-execution-screen.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ObjectiveElementExecutionScreenComponent implements OnDestroy {
  readonly store = inject(JourneyWorkspaceStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly oeGuid = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('oeGuid'))),
    { initialValue: null },
  );

  constructor() {
    effect(() => {
      const guid = this.oeGuid();
      // Re-run when workspace / standards load so `tryApplyRouteObjectiveElement` can resolve the OE.
      // Do not subscribe to broad computeds (e.g. filteredChapters) — that re-fired this effect on every
      // unrelated snapshot tweak and caused duplicate Journey + Knowledge Base HTTP calls.
      this.store.loading();
      this.store.standardsLoadingChapterId();
      this.store.snapshot();

      if (guid == null || guid === '') {
        void this.navigateBackToExecution();
        return;
      }

      const result = this.store.tryApplyRouteObjectiveElement(guid);
      if (result === 'loading') return;
      if (result === 'not_found') {
        void this.navigateBackToExecution();
        return;
      }
      if (result === 'applied' && !this.store.oeWizardOpen()) {
        this.store.openOeWizard();
      }
    });
  }

  ngOnDestroy(): void {
    if (this.store.oeWizardOpen()) {
      this.store.closeOeWizardWithoutConfirm();
    }
  }

  stageLabel(stage: string): string {
    const labels: Record<string, string> = {
      core: 'Core', commitment: 'Commitment', achievement: 'Achievement', excellence: 'Excellence',
    };
    return labels[stage] ?? stage;
  }

  /** Navigate back to execution screen, preserving the active stage filter as a query param. */
  navigateBackToExecution(): Promise<boolean> {
    const stage = this.store.maturityStageFilter();
    return stage
      ? this.router.navigate(['/app/journey/execution'], { queryParams: { stage } })
      : this.router.navigate(['/app/journey/execution']);
  }
}
