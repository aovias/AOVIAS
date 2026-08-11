import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExecutionRiskTone } from '../../models/execution-screen-vm.model';
import { StandardCardComponent } from '../standard-card/standard-card.component';
import { StandardModel } from '../standard-card/standard-card.model';

@Component({
  selector: 'app-standards-panel',
  standalone: true,
  imports: [CommonModule, StandardCardComponent],
  templateUrl: './standards-panel.component.html',
  styleUrl: './standards-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StandardsPanelComponent {
  readonly title = input.required<string>();
  readonly subtitle = input.required<string>();
  readonly complianceSummary = input.required<string>();
  readonly statusBadge = input.required<string>();
  readonly openIssues = input.required<number>();
  readonly standards = input.required<StandardModel[]>();
  readonly expandedIds = input.required<ReadonlySet<string>>();
  readonly showDepartmentColumn = input(false);
  readonly chapterRisk = input<ExecutionRiskTone>('low');

  readonly standardToggle = output<string>();
  readonly rowActivate = output<{ standardId: string; oeId: string }>();
}
