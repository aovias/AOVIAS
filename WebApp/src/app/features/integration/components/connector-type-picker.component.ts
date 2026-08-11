import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-connector-type-picker',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="connector-type-picker">
      <label>Provider type</label>
      <select (change)="onChange($event)">
        <option value="">Select connector type</option>
        @for (t of types(); track t) { <option [value]="t">{{ t }}</option> }
      </select>
    </div>
  `,
})
export class ConnectorTypePickerComponent {
  types = input<string[]>([]);
  selected = output<string>();
  onChange(ev: Event): void {
    const value = (ev.target as HTMLSelectElement).value;
    if (value) this.selected.emit(value);
  }
}
