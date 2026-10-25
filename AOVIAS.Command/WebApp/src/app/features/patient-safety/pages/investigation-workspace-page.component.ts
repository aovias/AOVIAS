import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { IncidentApiService } from '../services/incident-api.service';
import type { IncidentDetail } from '../models/incident.models';

@Component({
  selector: 'app-investigation-workspace-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="ps-page ps-page--narrow">
      <header class="page-header"><h1>Investigation Workspace</h1></header>
      @if (incident()) {
        <p class="context">Investigating: <strong>{{ incident()!.incidentNumber }}</strong> — {{ incident()!.title }}</p>
        <form class="form panel-card" (ngSubmit)="save()">
          <label>Incident Summary<textarea [(ngModel)]="summary" name="summary" rows="3"></textarea></label>
          <label>Contributing Factors (JSON)<textarea [(ngModel)]="contributingFactors" name="cf" rows="3"></textarea></label>
          <label>People Factors<textarea [(ngModel)]="peopleFactors" name="people" rows="2"></textarea></label>
          <label>Environment Factors<textarea [(ngModel)]="environmentFactors" name="env" rows="2"></textarea></label>
          <label>Equipment Factors<textarea [(ngModel)]="equipmentFactors" name="equip" rows="2"></textarea></label>
          <label>Process Factors<textarea [(ngModel)]="processFactors" name="proc" rows="2"></textarea></label>
          <label>Communication Factors<textarea [(ngModel)]="communicationFactors" name="comm" rows="2"></textarea></label>
          <label>Recommendations<textarea [(ngModel)]="recommendations" name="rec" rows="3"></textarea></label>
          <button type="submit" class="btn-primary">Save Investigation</button>
        </form>
      } @else {
        <p class="muted">Select an incident from the Command Center to begin investigation.</p>
      }
    </div>
  `,
})
export class InvestigationWorkspacePageComponent implements OnInit {
  private readonly api = inject(IncidentApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly aiContext = inject(AiPageContextService);
  readonly incident = signal<IncidentDetail | null>(null);
  summary = '';
  contributingFactors = '';
  peopleFactors = '';
  environmentFactors = '';
  equipmentFactors = '';
  processFactors = '';
  communicationFactors = '';
  recommendations = '';

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'PatientSafety', entityType: 'Investigation' });
    this.route.queryParams.subscribe((p) => {
      const id = p['incidentId'];
      if (id) this.load(id);
    });
  }

  load(id: string): void {
    this.api.getById(id).subscribe((d) => {
      this.incident.set(d);
      const inv = d.investigation;
      if (inv) {
        this.summary = inv.incidentSummary ?? '';
        this.contributingFactors = inv.contributingFactorsJson ?? '';
        this.peopleFactors = inv.peopleFactorsJson ?? '';
        this.environmentFactors = inv.environmentFactorsJson ?? '';
        this.equipmentFactors = inv.equipmentFactorsJson ?? '';
        this.processFactors = inv.processFactorsJson ?? '';
        this.communicationFactors = inv.communicationFactorsJson ?? '';
        this.recommendations = inv.recommendations ?? '';
      }
    });
  }

  save(): void {
    const inc = this.incident();
    if (!inc) return;
    this.api.saveInvestigation({
      incidentId: inc.id,
      incidentSummary: this.summary,
      contributingFactorsJson: this.contributingFactors,
      peopleFactorsJson: this.peopleFactors,
      environmentFactorsJson: this.environmentFactors,
      equipmentFactorsJson: this.equipmentFactors,
      processFactorsJson: this.processFactors,
      communicationFactorsJson: this.communicationFactors,
      recommendations: this.recommendations,
      status: 'InProgress',
    }).subscribe(() => this.load(inc.id));
  }
}
