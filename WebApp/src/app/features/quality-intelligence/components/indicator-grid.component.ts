import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import type { ClinicalIndicatorListItem } from '../models/quality-indicator.models';
import { formatQualityLabel, thresholdClass } from '../models/quality-indicator.models';

@Component({
  selector: 'app-indicator-grid',
  standalone: true,
  imports: [CommonModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="aov-table-wrap praman-scrollbar praman-scrollbar--dense">
      <table class="aov-table aov-table--wide">
        <thead>
          <tr>
            <th scope="col" (click)="sort.emit('code')">Code</th>
            <th scope="col" (click)="sort.emit('name')">Indicator</th>
            <th scope="col" (click)="sort.emit('category')">Category</th>
            <th scope="col" (click)="sort.emit('department')">Department</th>
            <th scope="col" (click)="sort.emit('value')">Current</th>
            <th scope="col">Target</th>
            <th scope="col" (click)="sort.emit('threshold')">Status</th>
            <th scope="col" (click)="sort.emit('trend')">Trend</th>
            <th scope="col">Frequency</th>
            <th scope="col">Updated</th>
          </tr>
        </thead>
        <tbody>
          @for (row of items(); track row.id) {
            <tr
              class="is-selectable"
              [attr.aria-selected]="selectedId() === row.id"
              (click)="select.emit(row.id)"
            >
              <td class="num">{{ row.indicatorCode }}</td>
              <td>
                <div class="title">{{ row.name }}</div>
              </td>
              <td>{{ formatLabel(row.category) }}</td>
              <td>{{ formatLabel(row.department) }}</td>
              <td>{{ row.currentValue ?? '—' }}</td>
              <td>{{ row.targetValue ?? '—' }}</td>
              <td>
                <span [class]="thresholdClass(row.thresholdStatus)">{{
                  formatLabel(row.thresholdStatus)
                }}</span>
              </td>
              <td>{{ formatLabel(row.trend) }}</td>
              <td>{{ formatLabel(row.frequency) }}</td>
              <td>{{ row.lastCalculatedAt | date: 'mediumDate' }}</td>
            </tr>
          } @empty {
            <tr>
              <td colspan="10" class="empty">No indicators match the current filters.</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class IndicatorGridComponent {
  readonly items = input<ClinicalIndicatorListItem[]>([]);
  readonly selectedId = input<string | null>(null);
  readonly sort = output<string>();
  readonly select = output<string>();
  readonly formatLabel = formatQualityLabel;
  readonly thresholdClass = thresholdClass;
}
