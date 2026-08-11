import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-sync-run-status-chip',
  standalone: true,
  imports: [CommonModule],
  template: `<span class="sync-run-status-chip" [attr.data-status]="status()">{{ status() }}</span>`,
  styles: [`[data-status="Completed"] { color: green; } [data-status="Failed"] { color: red; }`],
})
export class SyncRunStatusChipComponent {
  status = input('Unknown');
}
