import { Component, ChangeDetectionStrategy, EventEmitter, Output, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="file-upload-container">
      <input
        type="file"
        #fileInput
        [multiple]="multiple"
        [accept]="accept"
        (change)="onFileSelected($event)"
        style="display: none"
      />
      <button mat-raised-button (click)="fileInput.click()">
        <mat-icon>cloud_upload</mat-icon>
        {{ buttonText }}
      </button>
      <mat-progress-bar
        *ngIf="uploadProgress > 0 && uploadProgress < 100"
        mode="determinate"
        [value]="uploadProgress"
      >
      </mat-progress-bar>
    </div>
  `,
  styles: [
    `
      .file-upload-container {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }
    `,
  ],
})
export class FileUploadComponent {
  @Output() fileSelected = new EventEmitter<FileList>();
  @Output() uploadComplete = new EventEmitter<void>();

  @Input() multiple = false;
  @Input() accept = '*/*';
  @Input() buttonText = 'Upload File';
  uploadProgress = 0;

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.fileSelected.emit(input.files);
    }
  }
}
