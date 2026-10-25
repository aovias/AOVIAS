import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EmploymentApiService, EmploymentDetail } from '../services/employment-api.service';

type Tab = 'assignment' | 'position' | 'department' | 'reporting' | 'history' | 'timeline';

@Component({
  selector: 'app-employment-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    @if (employment(); as e) {
      <section class="employment-detail">
        <a routerLink="/app/workforce">← Directory</a>
        <h1>{{ e.personSearchName }}</h1>
        <p>{{ e.employmentNumber }} · <a [routerLink]="['/app/persons', e.personId]">View person</a></p>
        <nav class="tabs">
          @for (t of tabs; track t.id) {
            <button type="button" [class.active]="tab()===t.id" (click)="tab.set(t.id)">{{ t.label }}</button>
          }
        </nav>
        @switch (tab()) {
          @case ('assignment') { <p>Org: {{ e.organizationId }} · Hospital: {{ e.hospitalId }}</p> }
          @case ('position') { <ul>@for (p of e.positionAssignments; track p.id) {<li>{{ p.positionName }} ({{ p.isPrimary ? 'primary' : 'secondary' }})</li>}</ul> }
          @case ('department') { <ul>@for (d of e.departmentAssignments; track d.id) {<li>{{ d.masterDepartmentId }} — {{ d.allocationPercentage }}%</li>}</ul> }
          @case ('reporting') { <ul>@for (r of e.reportingRelationships; track r.id) {<li>{{ r.managerPersonSearchName || r.managerEmploymentId }}</li>}</ul> }
          @case ('history') { <ul>@for (h of history(); track h.id) {<li>{{ h.occurredAt | date:'medium' }} — {{ h.eventType }}</li>}</ul> }
          @case ('timeline') { <ul>@for (t of timeline(); track t.id) {<li>{{ t.occurredAt | date:'medium' }} — {{ t.summary }}</li>}</ul> }
        }
        <p>
          <a [routerLink]="['/app/workforce', e.id, 'transfer']">Transfer</a> ·
          <a [routerLink]="['/app/workforce', e.id, 'promote']">Promote</a>
        </p>
      </section>
    }
  `,
  styles: [`.employment-detail { padding:1.5rem; } .tabs button { margin-right:.25rem; } .tabs button.active { font-weight:bold; }`],
})
export class EmploymentDetailComponent implements OnInit {
  private readonly api = inject(EmploymentApiService);
  private readonly route = inject(ActivatedRoute);
  employment = signal<EmploymentDetail | null>(null);
  timeline = signal<{ id: string; occurredAt: string; summary: string }[]>([]);
  history = signal<{ id: string; occurredAt: string; eventType: string }[]>([]);
  tab = signal<Tab>('assignment');
  tabs: { id: Tab; label: string }[] = [
    { id: 'assignment', label: 'Organization' },
    { id: 'position', label: 'Position' },
    { id: 'department', label: 'Department' },
    { id: 'reporting', label: 'Reporting' },
    { id: 'history', label: 'History' },
    { id: 'timeline', label: 'Timeline' },
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.api.getById(id).subscribe({ next: (e) => this.employment.set(e) });
    this.api.getTimeline(id).subscribe({ next: (t) => this.timeline.set(t) });
    this.api.getHistory(id).subscribe({ next: (h) => this.history.set(h) });
  }
}
