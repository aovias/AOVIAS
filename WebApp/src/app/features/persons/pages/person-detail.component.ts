import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PersonApiService, PersonDetail } from '../services/person-api.service';

type Tab = 'profile' | 'contacts' | 'addresses' | 'documents' | 'preferences' | 'timeline';

@Component({
  selector: 'app-person-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="person-detail" *ngIf="person() as p">
      <header>
        <a routerLink="/app/persons">← Directory</a>
        <h1>{{ p.displayName }}</h1>
        <span class="badge">{{ p.platformId }} · {{ p.status }}</span>
      </header>

      <nav class="tabs">
        @for (t of tabs; track t.id) {
          <button type="button" [class.active]="activeTab() === t.id" (click)="activeTab.set(t.id)">
            {{ t.label }}
          </button>
        }
      </nav>

      @switch (activeTab()) {
        @case ('profile') {
          <dl class="grid">
            <dt>Legal name</dt><dd>{{ p.legalFirstName }} {{ p.legalLastName }}</dd>
            <dt>Preferred name</dt><dd>{{ p.preferredName || '—' }}</dd>
            <dt>Date of birth</dt><dd>{{ p.dateOfBirth || '—' }}</dd>
            <dt>Time zone</dt><dd>{{ p.timeZone || '—' }}</dd>
          </dl>
        }
        @case ('contacts') {
          <ul>
            @for (c of p.contacts; track c.id) {
              <li>{{ c.contactKind }}: {{ c.value }} @if (c.isPrimary) { (primary) }</li>
            }
          </ul>
          <h3>Emergency contacts</h3>
          <ul>
            @for (e of p.emergencyContacts; track e.id) {
              <li>{{ e.name }} — {{ e.phone || e.email }}</li>
            }
          </ul>
        }
        @case ('addresses') {
          <ul>
            @for (a of p.addresses; track a.id) {
              <li>{{ a.line1 }}, {{ a.city }} {{ a.postalCode }}</li>
            }
          </ul>
        }
        @case ('documents') {
          <ul>
            @for (d of p.documents; track d.id) {
              <li>{{ d.documentNumber }} ({{ d.verificationStatus }})</li>
            }
          </ul>
        }
        @case ('preferences') {
          @if (p.preferences; as prefs) {
            <dl class="grid">
              <dt>Theme</dt><dd>{{ prefs.theme }}</dd>
              <dt>Language</dt><dd>{{ prefs.languageCode || '—' }}</dd>
              <dt>Date format</dt><dd>{{ prefs.dateFormat }}</dd>
            </dl>
          } @else {
            <p>No preferences recorded.</p>
          }
        }
        @case ('timeline') {
          <ul class="timeline">
            @for (e of timeline(); track e.id) {
              <li>
                <time>{{ e.occurredAt | date: 'medium' }}</time>
                <strong>{{ e.eventType }}</strong> — {{ e.summary }}
              </li>
            }
          </ul>
        }
      }
    </section>
    @if (loading()) { <p>Loading person...</p> }
    @if (error()) { <p class="error">{{ error() }}</p> }
  `,
  styles: [
    `
      .person-detail { padding: 1.5rem; }
      header { margin-bottom: 1rem; }
      .badge { color: #6b7280; margin-left: 0.5rem; }
      .tabs { display: flex; gap: 0.25rem; margin-bottom: 1rem; flex-wrap: wrap; }
      .tabs button { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; background: #fff; cursor: pointer; }
      .tabs button.active { background: #111827; color: #fff; border-color: #111827; }
      .grid { display: grid; grid-template-columns: 10rem 1fr; gap: 0.5rem 1rem; }
      .timeline { list-style: none; padding: 0; }
      .timeline li { padding: 0.5rem 0; border-bottom: 1px solid #f3f4f6; }
      .error { color: #b91c1c; }
    `,
  ],
})
export class PersonDetailComponent implements OnInit {
  private readonly api = inject(PersonApiService);
  private readonly route = inject(ActivatedRoute);

  person = signal<PersonDetail | null>(null);
  timeline = signal<{ id: string; occurredAt: string; eventType: string; summary: string }[]>([]);
  activeTab = signal<Tab>('profile');
  loading = signal(true);
  error = signal<string | null>(null);

  readonly tabs: { id: Tab; label: string }[] = [
    { id: 'profile', label: 'Personal' },
    { id: 'contacts', label: 'Contacts' },
    { id: 'addresses', label: 'Addresses' },
    { id: 'documents', label: 'Documents' },
    { id: 'preferences', label: 'Preferences' },
    { id: 'timeline', label: 'Timeline' },
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Person not found.');
      this.loading.set(false);
      return;
    }

    this.api.getById(id).subscribe({
      next: (detail) => {
        this.person.set(detail);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load person.');
        this.loading.set(false);
      },
    });

    this.api.getTimeline(id).subscribe({
      next: (entries) => this.timeline.set(entries),
      error: () => this.timeline.set([]),
    });
  }
}
