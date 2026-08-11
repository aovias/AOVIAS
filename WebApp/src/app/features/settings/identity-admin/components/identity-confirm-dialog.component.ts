import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

export interface IdentityConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  destructive?: boolean;
}

@Component({
  selector: 'app-identity-confirm-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content
      ><p>{{ data.message }}</p></mat-dialog-content
    >
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="dialogRef.close(false)">Cancel</button>
      <button
        mat-flat-button
        [color]="data.destructive ? 'warn' : 'primary'"
        type="button"
        (click)="dialogRef.close(true)"
      >
        {{ data.confirmLabel || 'Confirm' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class IdentityConfirmDialogComponent {
  readonly data = inject<IdentityConfirmDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<IdentityConfirmDialogComponent, boolean>);
}
