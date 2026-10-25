import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { IncidentApiService } from '../services/incident-api.service';
import {
  ROOT_CAUSE_CATEGORIES,
  emptyCauseCategories,
  emptyFishbone,
  type CauseCategories,
  type FishboneSchema,
  type FiveWhys,
  type IncidentDetail,
} from '../models/incident.models';

@Component({
  selector: 'app-rca-workspace-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="ps-page ps-page--medium">
      <header class="page-header">
        <div>
          <h1>Root Cause Analysis</h1>
          <p class="subtitle">Guided RCA — 5 Whys, cause categories, fishbone (future-ready)</p>
        </div>
      </header>

      @if (incident(); as inc) {
        <p class="context">{{ inc.incidentNumber }} — {{ inc.title }}</p>
        @if (inc.status === 'Closed' || inc.status === 'Cancelled') {
          <p class="muted">This incident is {{ inc.status }} and cannot be modified.</p>
        }

        <form class="form panel-card rca-form" (ngSubmit)="save()">
          <label>Method
            <select [(ngModel)]="method" name="method" [disabled]="readOnly">
              <option value="FiveWhys">5 Whys</option>
              <option value="Fishbone">Fishbone (Ishikawa)</option>
              <option value="ContributingFactors">Contributing Factors</option>
            </select>
          </label>

          <label>Root cause category
            <select [(ngModel)]="rootCauseCategory" name="rootCauseCategory" [disabled]="readOnly">
              <option value="">— Select —</option>
              @for (c of rootCauseCategories; track c.value) {
                <option [value]="c.value">{{ c.label }}</option>
              }
            </select>
          </label>

          @if (method === 'FiveWhys') {
            <fieldset class="rca-fieldset">
              <legend>5 Whys</legend>
              <label>Why 1?<input type="text" [(ngModel)]="fiveWhys.why1" name="why1" [disabled]="readOnly" /></label>
              <label>Why 2?<input type="text" [(ngModel)]="fiveWhys.why2" name="why2" [disabled]="readOnly" /></label>
              <label>Why 3?<input type="text" [(ngModel)]="fiveWhys.why3" name="why3" [disabled]="readOnly" /></label>
              <label>Why 4?<input type="text" [(ngModel)]="fiveWhys.why4" name="why4" [disabled]="readOnly" /></label>
              <label>Why 5?<input type="text" [(ngModel)]="fiveWhys.why5" name="why5" [disabled]="readOnly" /></label>
              <label>Final root cause<textarea [(ngModel)]="fiveWhys.finalRootCause" name="finalRoot" rows="2" [disabled]="readOnly"></textarea></label>
            </fieldset>
          }

          @if (method === 'Fishbone') {
            <fieldset class="rca-fieldset">
              <legend>Fishbone diagram</legend>
              <label>Effect / problem<input type="text" [(ngModel)]="fishbone.effect" name="effect" [disabled]="readOnly" /></label>
              @for (cat of fishbone.categories; track cat.name; let i = $index) {
                <label>{{ cat.name }} causes
                  <input
                    type="text"
                    [ngModel]="cat.causes.join(', ')"
                    (ngModelChange)="setFishboneCauses(i, $event)"
                    [name]="'fish_' + i"
                    [disabled]="readOnly"
                    placeholder="Comma-separated causes"
                  />
                </label>
              }
            </fieldset>
          }

          <fieldset class="rca-fieldset">
            <legend>Cause categories</legend>
            <label>Human factors
              <input type="text" [ngModel]="causeText.human" (ngModelChange)="causeText.human = $event" name="human" [disabled]="readOnly" placeholder="Comma-separated" />
            </label>
            <label>Process factors
              <input type="text" [ngModel]="causeText.process" (ngModelChange)="causeText.process = $event" name="process" [disabled]="readOnly" placeholder="Comma-separated" />
            </label>
            <label>Environmental factors
              <input type="text" [ngModel]="causeText.environmental" (ngModelChange)="causeText.environmental = $event" name="env" [disabled]="readOnly" placeholder="Comma-separated" />
            </label>
            <label>Equipment factors
              <input type="text" [ngModel]="causeText.equipment" (ngModelChange)="causeText.equipment = $event" name="equip" [disabled]="readOnly" placeholder="Comma-separated" />
            </label>
            <label>Organization factors
              <input type="text" [ngModel]="causeText.organization" (ngModelChange)="causeText.organization = $event" name="org" [disabled]="readOnly" placeholder="Comma-separated" />
            </label>
          </fieldset>

          <label>Root cause summary<textarea [(ngModel)]="rootCauseSummary" name="summary" rows="3" [disabled]="readOnly"></textarea></label>
          <label>Corrective action (→ deficiency CAPA on promote)
            <textarea [(ngModel)]="correctiveAction" name="corrective" rows="2" [disabled]="readOnly"></textarea>
          </label>
          <label>Preventive action (→ deficiency CAPA on promote)
            <textarea [(ngModel)]="preventiveAction" name="preventive" rows="2" [disabled]="readOnly"></textarea>
          </label>

          @if (!readOnly) {
            <button type="submit" class="btn-primary">Save RCA</button>
          }
        </form>
      } @else {
        <p class="muted">Select an incident from the Command Center to perform RCA.</p>
      }
    </div>
  `,
})
export class RcaWorkspacePageComponent implements OnInit {
  private readonly api = inject(IncidentApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly aiContext = inject(AiPageContextService);

  readonly incident = signal<IncidentDetail | null>(null);
  readonly rootCauseCategories = ROOT_CAUSE_CATEGORIES;

  method = 'FiveWhys';
  rootCauseCategory = '';
  fiveWhys: FiveWhys = {};
  fishbone: FishboneSchema = emptyFishbone();
  causeText = { human: '', process: '', environmental: '', equipment: '', organization: '' };
  rootCauseSummary = '';
  correctiveAction = '';
  preventiveAction = '';

  get readOnly(): boolean {
    const s = this.incident()?.status;
    return s === 'Closed' || s === 'Cancelled';
  }

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'PatientSafety', entityType: 'RCA' });
    this.route.queryParams.subscribe((p) => {
      if (p['incidentId']) this.load(p['incidentId']);
    });
  }

  load(id: string): void {
    this.api.getById(id).subscribe((d) => {
      this.incident.set(d);
      const rca = d.rootCauseAnalysis;
      if (!rca) {
        this.resetForm(d.title);
        return;
      }
      this.method = rca.method || 'FiveWhys';
      this.rootCauseCategory = rca.rootCauseCategory ?? '';
      this.fiveWhys = { ...(rca.fiveWhys ?? {}) };
      this.fishbone = rca.fishbone?.categories?.length ? { ...rca.fishbone, categories: rca.fishbone.categories.map((c) => ({ ...c, causes: [...c.causes] })) } : emptyFishbone(d.title);
      const cats = rca.causeCategories ?? emptyCauseCategories();
      this.causeText = {
        human: (cats.human ?? []).join(', '),
        process: (cats.process ?? []).join(', '),
        environmental: (cats.environmental ?? []).join(', '),
        equipment: (cats.equipment ?? []).join(', '),
        organization: (cats.organization ?? []).join(', '),
      };
      this.rootCauseSummary = rca.rootCauseSummary ?? '';
      this.correctiveAction = rca.correctiveActionSummary ?? '';
      this.preventiveAction = rca.preventiveActionSummary ?? '';
    });
  }

  setFishboneCauses(index: number, value: string): void {
    this.fishbone.categories[index].causes = splitCsv(value);
  }

  save(): void {
    const inc = this.incident();
    if (!inc || this.readOnly) return;

    const causeCategories: CauseCategories = {
      human: splitCsv(this.causeText.human),
      process: splitCsv(this.causeText.process),
      environmental: splitCsv(this.causeText.environmental),
      equipment: splitCsv(this.causeText.equipment),
      organization: splitCsv(this.causeText.organization),
    };

    this.api
      .saveRca({
        incidentId: inc.id,
        method: this.method,
        fiveWhys: this.method === 'FiveWhys' ? this.fiveWhys : undefined,
        fishbone: this.method === 'Fishbone' ? this.fishbone : undefined,
        causeCategories,
        rootCauseSummary: this.rootCauseSummary,
        correctiveActionSummary: this.correctiveAction,
        preventiveActionSummary: this.preventiveAction,
        rootCauseCategory: this.rootCauseCategory || undefined,
      })
      .subscribe(() => this.load(inc.id));
  }

  private resetForm(title: string): void {
    this.method = 'FiveWhys';
    this.rootCauseCategory = '';
    this.fiveWhys = {};
    this.fishbone = emptyFishbone(title);
    this.causeText = { human: '', process: '', environmental: '', equipment: '', organization: '' };
    this.rootCauseSummary = '';
    this.correctiveAction = '';
    this.preventiveAction = '';
  }
}

function splitCsv(value: string): string[] {
  return value
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean);
}
