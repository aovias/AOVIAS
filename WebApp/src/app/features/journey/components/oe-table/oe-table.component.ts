import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExecutionOeRowVm } from '../../models/journey-execution-vm.model';
import { OeRowComponent } from '../oe-row/oe-row.component';

@Component({
  selector: 'app-oe-table',
  standalone: true,
  imports: [CommonModule, OeRowComponent],
  templateUrl: './oe-table.component.html',
  styleUrl: './oe-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OeTableComponent {
  readonly rows = input.required<ExecutionOeRowVm[]>();
  readonly rowView = output<string>();
}
