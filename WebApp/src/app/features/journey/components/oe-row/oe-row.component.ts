import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExecutionOeRowVm } from '../../models/journey-execution-vm.model';

@Component({
  selector: 'app-oe-row',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './oe-row.component.html',
  styleUrl: './oe-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OeRowComponent {
  readonly row = input.required<ExecutionOeRowVm>();
  readonly view = output<string>();

  onView(): void {
    this.view.emit(this.row().id);
  }
}
