import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { IncidentApiService } from '../services/incident-api.service';
import { IncidentGridComponent } from '../components/incident-grid.component';
import type { IncidentListItem } from '../models/incident.models';

@Component({
  selector: 'app-filtered-incidents-page',
  standalone: true,
  imports: [CommonModule, IncidentGridComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header"><h1>{{ title() }}</h1></header>
      <app-incident-grid [items]="items()" [selectedId]="null" (select)="onSelect($event)" />
    </div>
  `,
})
export class FilteredIncidentsPageComponent implements OnInit {
  private readonly api = inject(IncidentApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly title = signal('');
  readonly quickFilter = signal('');
  readonly items = signal<IncidentListItem[]>([]);

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'PatientSafety' });
  }

  init(title: string, quickFilter: string): void {
    this.title.set(title);
    this.quickFilter.set(quickFilter);
    this.api.getIncidents({ quickFilter }).subscribe((l) => this.items.set(l.items));
  }

  onSelect(_id: string): void { /* navigate via command center detail */ }
}
