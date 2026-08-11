import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { OeHistoryTimelineComponent } from '../components/oe-history-timeline/oe-history-timeline.component';
import type { OeHistoryEvent } from '../models/objective-element.model';

@Component({
  selector: 'app-oe-history-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, OeHistoryTimelineComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>History — {{ data.oeCode }}</h2>
    <mat-dialog-content>
      <app-oe-history-timeline [events]="data.events" />
    </mat-dialog-content>
  `,
})
export class OeHistoryDialogComponent {
  readonly data = inject<{ oeCode: string; events: OeHistoryEvent[] }>(MAT_DIALOG_DATA);
}

export function openOeHistoryDialog(
  dialog: MatDialog,
  oeCode: string,
  events: OeHistoryEvent[],
): void {
  dialog.open(OeHistoryDialogComponent, {
    width: 'min(520px, 96vw)',
    data: { oeCode, events },
  });
}
