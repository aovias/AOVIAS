import { Component, ChangeDetectionStrategy, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-form-field',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-form-field [appearance]="appearance" [class]="class">
      <mat-label>{{ label }}</mat-label>
      <input
        matInput
        [formControl]="control"
        [type]="type"
        [placeholder]="placeholder"
        [required]="required"
      />
      <mat-error *ngIf="control.hasError('required')">
        {{ errorMessage || label + ' is required' }}
      </mat-error>
    </mat-form-field>
  `,
  styles: [
    `
      mat-form-field {
        width: 100%;
      }
    `,
  ],
})
export class FormFieldComponent {
  @Input() control!: FormControl;
  @Input() label = '';
  @Input() type = 'text';
  @Input() placeholder = '';
  @Input() required = false;
  @Input() appearance: 'fill' | 'outline' = 'outline';
  @Input() class = '';
  @Input() errorMessage = '';
}
