import { Component, ChangeDetectionStrategy, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
  selector: 'app-progress-indicator',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="progress-container">
      <mat-spinner *ngIf="mode === 'spinner'" [diameter]="diameter"></mat-spinner>
      <mat-progress-bar
        *ngIf="mode === 'bar'"
        [mode]="determinate ? 'determinate' : 'indeterminate'"
        [value]="value"
      >
      </mat-progress-bar>
    </div>
  `,
  styles: [
    `
      .progress-container {
        display: flex;
        justify-content: center;
        align-items: center;
        padding: 1rem;
      }
    `,
  ],
})
export class ProgressIndicatorComponent {
  @Input() mode: 'spinner' | 'bar' = 'spinner';
  @Input() diameter = 50;
  @Input() determinate = false;
  @Input() value = 0;
}
