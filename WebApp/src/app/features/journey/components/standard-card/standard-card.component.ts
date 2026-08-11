import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { ScrollingModule } from '@angular/cdk/scrolling';
import {
  lucideAlertTriangle,
  lucideChevronDown,
  lucideCircleAlert,
  lucideCircleX,
  lucideCheck,
} from '@ng-icons/lucide';
import { ExecutionRiskTone } from '../../models/execution-screen-vm.model';
import { JourneyOperationalModeUiPolicy } from '../../models/journey-operational-mode-ui.model';
import { StandardCardOeModel, StandardModel } from './standard-card.model';
import { environment } from '../../../../../environments/environment';
import { JourneyWorkspaceStore } from '../../state/journey-workspace.store';
import { ObligationReadinessBarComponent } from '../obligation-readiness-bar/obligation-readiness-bar.component';
import { ObligationStateApiDto } from '../../models/journey-api.model';

const SCORE_CIRC = 97.39;

@Component({
  selector: 'app-standard-card',
  standalone: true,
  imports: [CommonModule, NgIcon, ScrollingModule, ObligationReadinessBarComponent],
  viewProviders: [
    provideIcons({
      lucideChevronDown,
      lucideCheck,
      lucideAlertTriangle,
      lucideCircleX,
      lucideCircleAlert,
    }),
  ],
  templateUrl: './standard-card.component.html',
  styleUrl: './standard-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StandardCardComponent {
  readonly standard = input.required<StandardModel>();
  readonly modeUi = input<JourneyOperationalModeUiPolicy | null>(null);

  private readonly store = inject(JourneyWorkspaceStore);

  obligationStateFor(oeId: string): ObligationStateApiDto | null {
    return this.store.obligationStateByOeId().get(oeId) ?? null;
  }
  readonly isExpanded = input(false);
  readonly isActive = input(false);
  /** When collapsed, left accent matches chapter-item risk bands. */
  readonly chapterRisk = input<ExecutionRiskTone>('low');
  readonly showDepartmentColumn = input(true);

  readonly toggle = output<string>();
  /** Row activated (click/keyboard) — parent selects OE for context panel. */
  readonly rowActivate = output<{ standardId: string; oeId: string }>();
  readonly useVirtualOeRows = environment.journeyVirtualScrollObjectiveElementsEnabled;

  /** Virtual scroll needs a stable viewport; use a plain list for typical OE counts. */
  get useVirtualOeScroll(): boolean {
    return this.useVirtualOeRows && (this.standard()?.objectiveElements?.length ?? 0) > 40;
  }

  readonly oeVirtualRowSize = 64;

  readonly coreBadge = computed(() => {
    const p = this.standard().compliancePercent;
    if (p >= 75) {
      return { tone: 'compliant' as const, label: 'CORE COMPLIANT' };
    }
    if (p >= 40) {
      return { tone: 'partial' as const, label: 'CORE PARTIAL' };
    }
    return { tone: 'non-compliant' as const, label: 'CORE NON-COMPLIANT' };
  });

  readonly progressDashOffset = computed(() => {
    const pct = Math.min(100, Math.max(0, this.standard().compliancePercent));
    return SCORE_CIRC - (SCORE_CIRC * pct) / 100;
  });

  onHeaderClick(): void {
    this.toggle.emit(this.standard().id);
  }

  onHeaderKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.onHeaderClick();
    }
  }

  onOeRowActivate(oe: StandardCardOeModel): void {
    this.rowActivate.emit({ standardId: this.standard().id, oeId: oe.id });
  }

  scoreTierClass(scoreLabel: string): string {
    const n = parseInt(/(\d)/.exec(scoreLabel)?.[1] ?? '', 10);
    if (Number.isNaN(n) || n < 1 || n > 5) {
      return 'standard-card__score-pill--na';
    }
    return `standard-card__score-pill--s${n}`;
  }

  trackByOeId(_index: number, oe: StandardCardOeModel): string {
    return oe.id;
  }
}
