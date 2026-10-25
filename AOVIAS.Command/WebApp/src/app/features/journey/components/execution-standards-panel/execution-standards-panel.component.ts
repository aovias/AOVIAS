import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ExecutionStandardItemVm,
  ExecutionStandardsPanelVm,
} from '../../models/execution-screen-vm.model';
import { JourneyOperationalModeUiPolicy } from '../../models/journey-operational-mode-ui.model';
import { JourneyWorkspaceStore, OeQuickFilter } from '../../state/journey-workspace.store';
import { StandardCardComponent } from '../standard-card/standard-card.component';
import { StandardModel, standardModelFromExecutionStandardItemVm } from '../standard-card/standard-card.model';

/** Department filter chips — aligned with reference execution workspace. */
export const EXECUTION_DEPARTMENT_FILTERS: { id: string; label: string }[] = [
  { id: 'all', label: 'All' },
  { id: 'clinical', label: 'Clinical' },
  { id: 'pharmacy', label: 'Pharmacy' },
  { id: 'hr', label: 'HR' },
  { id: 'facility', label: 'Facility' },
  { id: 'infection_control', label: 'Infection Control' },
];

@Component({
  selector: 'app-execution-standards-panel',
  standalone: true,
  imports: [CommonModule, StandardCardComponent],
  templateUrl: './execution-standards-panel.component.html',
  styleUrl: './execution-standards-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExecutionStandardsPanelComponent implements OnChanges {
  @Input({ required: true }) vm!: ExecutionStandardsPanelVm;
  @Input() modeUi: JourneyOperationalModeUiPolicy | null = null;

  readonly store = inject(JourneyWorkspaceStore);
  readonly departmentFilters = EXECUTION_DEPARTMENT_FILTERS;

  /** Which standard cards show the OE table — independent of selected standard (no forced expand). */
  readonly expandedStandardIds = signal<ReadonlySet<string>>(new Set());

  private lastItemsSignature = '';

  @Output() readonly standardSelect = new EventEmitter<string>();
  /** Select OE for execution context (right panel) — row tap / keyboard. */
  @Output() readonly oeContextSelect = new EventEmitter<{ standardId: string; oeId: string }>();

  readonly oeQuickFilters: { id: OeQuickFilter; label: string }[] = [
    { id: 'all', label: 'All' },
    { id: 'core_only', label: 'Core Only' },
    { id: 'non_compliant', label: 'Non-Compliant' },
    { id: 'high_risk', label: 'High Risk' },
  ];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['vm'] && this.vm?.items) {
      const sig = this.vm.items.map((s) => s.id).join('|');
      if (sig !== this.lastItemsSignature) {
        this.lastItemsSignature = sig;
        const withOes = this.vm.items.filter((s) => s.objectiveElements.length > 0);
        const activeWithOes = this.vm.items
          .filter((s) => s.isActive && s.objectiveElements.length > 0)
          .map((s) => s.id);
        const firstWithOes = withOes[0]?.id;
        const fallback = this.vm.items[0]?.id;
        const initial =
          activeWithOes.length > 0
            ? activeWithOes
            : firstWithOes
              ? [firstWithOes]
              : fallback
                ? [fallback]
                : [];
        this.expandedStandardIds.set(new Set(initial));
      }
    }
  }

  onStandardHeaderClick(standardId: string): void {
    this.expandedStandardIds.update((prev) => {
      const next = new Set(prev);
      if (next.has(standardId)) next.delete(standardId);
      else next.add(standardId);
      return next;
    });
    this.standardSelect.emit(standardId);
  }

  onRowActivate(ev: { standardId: string; oeId: string }): void {
    this.oeContextSelect.emit(ev);
  }

  asStandardModel(s: ExecutionStandardItemVm): StandardModel {
    return standardModelFromExecutionStandardItemVm(s);
  }

  onOeQuickFilter(id: OeQuickFilter): void {
    this.store.setOeQuickFilter(id);
  }

  isExpanded(standardId: string): boolean {
    return this.expandedStandardIds().has(standardId);
  }

  onDepartmentFilter(id: string): void {
    this.store.setDepartmentFilter(id);
  }
}
