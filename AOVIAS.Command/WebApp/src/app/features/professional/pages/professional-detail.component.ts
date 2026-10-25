import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProfessionalApiService, ProfessionalDetail } from '../services/professional-api.service';

type Tab = 'qualifications' | 'registrations' | 'licenses' | 'certifications' | 'privileges' | 'credentialing' | 'competencies' | 'training';

@Component({
  selector: 'app-professional-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    @if (profile(); as p) {
      <section>
        <a routerLink="/app/professional">← Directory</a>
        <h1>{{ p.personSearchName }}</h1>
        <p>{{ p.professionalNumber }} · <a [routerLink]="['/app/persons', p.personId]">Person</a> · <a [routerLink]="['/app/workforce', p.employmentId]">Employment</a> · <a [routerLink]="['/app/professional', p.id, 'timeline']">Timeline</a> · <a routerLink="/app/settings/teams/training">Training programs</a></p>
        <nav class="tabs">
          @for (t of tabs; track t.id) {
            <button type="button" [class.active]="tab()===t.id" (click)="tab.set(t.id)">{{ t.label }}</button>
          }
        </nav>
        @switch (tab()) {
          @case ('qualifications') { <ul>@for (q of p.qualifications; track q.id) {<li>{{ q.title }} @if (q.institution) { — {{ q.institution }} }</li>}</ul> }
          @case ('registrations') { <ul>@for (r of p.registrations; track r.id) {<li>{{ r.registrationNumber }} ({{ r.status }}) @if (r.expiryDate) { — expires {{ r.expiryDate | date:'mediumDate' }} }</li>}</ul> }
          @case ('licenses') { <ul>@for (l of p.licenses; track l.id) {<li>{{ l.licenseNumber }} ({{ l.status }})</li>}</ul> }
          @case ('certifications') { <ul>@for (c of p.certifications; track c.id) {<li>{{ c.certificationName }}</li>}</ul> }
          @case ('privileges') { <ul>@for (c of p.clinicalPrivileges; track c.id) {<li>{{ c.privilegeName }} — {{ c.status }}</li>}</ul> }
          @case ('credentialing') { <ul>@for (c of p.credentialingRecords; track c.id) {<li>{{ c.status }}</li>}</ul> }
          @case ('competencies') { <ul>@for (c of p.competencies; track c.id) {<li>{{ c.skillName }} @if (c.level) { ({{ c.level }}) }</li>}</ul> }
          @case ('training') {
            <form class="add-training" (ngSubmit)="addTraining(p.id)">
              <input [(ngModel)]="courseName" name="courseName" placeholder="Course name" required />
              <input [(ngModel)]="provider" name="provider" placeholder="Provider" />
              <label><input type="checkbox" [(ngModel)]="isMandatory" name="isMandatory" /> Mandatory</label>
              <button type="submit">Add training record</button>
            </form>
            @if (trainingError()) { <p class="err">{{ trainingError() }}</p> }
            <ul>@for (t of p.trainingRecords; track t.id) {<li>{{ t.courseName }} @if (t.completionDate) { — {{ t.completionDate | date:'mediumDate' }} }</li>}</ul>
          }
        }
      </section>
    }
  `,
  styles: [`.tabs button { margin-right:.25rem; } .tabs button.active { font-weight:bold; } section { padding:1.5rem; } .add-training { display:flex; flex-wrap:wrap; gap:.5rem; margin:.75rem 0; } .err { color:#b91c1c; }`],
})
export class ProfessionalDetailComponent implements OnInit {
  private readonly api = inject(ProfessionalApiService);
  private readonly route = inject(ActivatedRoute);
  profile = signal<ProfessionalDetail | null>(null);
  tab = signal<Tab>('qualifications');
  trainingError = signal<string | null>(null);
  courseName = '';
  provider = '';
  isMandatory = false;
  tabs: { id: Tab; label: string }[] = [
    { id: 'qualifications', label: 'Qualifications' },
    { id: 'registrations', label: 'Registrations' },
    { id: 'licenses', label: 'Licenses' },
    { id: 'certifications', label: 'Certifications' },
    { id: 'privileges', label: 'Clinical Privileges' },
    { id: 'credentialing', label: 'Credentialing' },
    { id: 'competencies', label: 'Competencies' },
    { id: 'training', label: 'Training' },
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.load(id);
  }

  addTraining(profileId: string): void {
    if (!this.courseName.trim()) return;
    this.trainingError.set(null);
    this.api.addTrainingRecord(profileId, {
      courseName: this.courseName.trim(),
      provider: this.provider.trim() || undefined,
      isMandatory: this.isMandatory,
      completionDate: new Date().toISOString(),
    }).subscribe({
      next: () => {
        this.courseName = '';
        this.provider = '';
        this.isMandatory = false;
        this.load(profileId);
      },
      error: () => this.trainingError.set('Failed to add training record.'),
    });
  }

  private load(id: string): void {
    this.api.getById(id).subscribe({ next: (p) => this.profile.set(p) });
  }
}
