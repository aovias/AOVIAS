import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { IncidentApiService } from '../services/incident-api.service';
import { IncidentGridComponent } from '../components/incident-grid.component';
import type { IncidentListItem } from '../models/incident.models';

@Component({
  selector: 'app-near-misses-page',
  standalone: true,
  imports: [CommonModule, IncidentGridComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header">
        <div>
          <h1>Near Misses</h1>
          <p class="subtitle">Proactive safety signals before harm occurs</p>
        </div>
      </header>
      <app-incident-grid [items]="items()" [selectedId]="null" (select)="open($event)" />
    </div>
  `,
})
export class NearMissesPageComponent implements OnInit {
  private readonly api = inject(IncidentApiService);
  private readonly router = inject(Router);
  private readonly aiContext = inject(AiPageContextService);
  readonly items = signal<IncidentListItem[]>([]);

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'PatientSafety' });
    this.api.getIncidents({ quickFilter: 'nearMiss' }).subscribe((l) => this.items.set(l.items));
  }

  open(id: string): void {
    this.router.navigate(['/app/patient-safety/command-center'], { queryParams: { selected: id } });
  }
}
