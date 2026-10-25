import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { validateEvidenceFilesForMode } from '../../journey/utils/oe-evidence-upload.constants';

@Component({
  selector: 'app-binder-evidence-upload',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="upload">
      <input #fileInput type="file" class="hidden" (change)="onFile($event)" />
      <input #camInput type="file" accept="image/*" capture="environment" class="hidden" (change)="onFile($event)" />
      <button mat-stroked-button type="button" [disabled]="uploading()" (click)="fileInput.click()">Upload file</button>
      <button mat-stroked-button type="button" [disabled]="uploading()" (click)="camInput.click()">Take photo</button>
      @if (uploading()) {
        <mat-spinner diameter="24" />
      }
    </div>
    @if (hint()) {
      <p class="hint">{{ hint() }}</p>
    }
  `,
  styles: [
    `
      .upload {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
        align-items: center;
        margin-bottom: 8px;
      }
      .hidden {
        display: none;
      }
      .hint {
        font-size: 12px;
        color: #b45309;
        margin: 0;
      }
    `,
  ],
})
export class EvidenceUploadComponent {
  private readonly snackBar = inject(MatSnackBar);

  readonly uploading = input(false);
  readonly filePicked = output<File>();
  readonly hint = input<string | null>(null);

  onFile(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const files = input.files ? Array.from(input.files) : [];
    input.value = '';
    const v = validateEvidenceFilesForMode(files, 'single');
    if (!v.ok) {
      this.snackBar.open(v.message, 'Dismiss', { duration: 5000 });
      return;
    }
    this.filePicked.emit(files[0]!);
  }
}
