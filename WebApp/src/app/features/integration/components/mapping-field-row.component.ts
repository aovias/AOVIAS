import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-mapping-field-row',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="mapping-field-row">
      <input
        type="text"
        [ngModel]="sourceField()"
        (ngModelChange)="sourceFieldChange.emit($event)"
        placeholder="Source field"
        aria-label="Source field"
      />
      <span aria-hidden="true">→</span>
      <input
        type="text"
        [ngModel]="targetField()"
        (ngModelChange)="targetFieldChange.emit($event)"
        placeholder="Target field"
        aria-label="Target field"
      />
    </div>
  `,
  styles: [
    `
      .mapping-field-row {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 0.5rem;
      }
      input {
        flex: 1;
        padding: 0.375rem 0.5rem;
      }
    `,
  ],
})
export class MappingFieldRowComponent {
  sourceField = input('');
  targetField = input('');
  sourceFieldChange = output<string>();
  targetFieldChange = output<string>();
}
