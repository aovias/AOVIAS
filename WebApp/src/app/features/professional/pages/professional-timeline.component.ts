import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProfessionalApiService } from '../services/professional-api.service';

@Component({
  selector: 'app-professional-timeline',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section style="padding:1.5rem">
      <a [routerLink]="['/app/professional', profileId]">← Profile</a>
      <h1>Professional Timeline</h1>
      <ul>
        @for (t of timeline(); track t.id) {
          <li>{{ t.occurredAt | date:'medium' }} — {{ t.summary }}</li>
        }
      </ul>
    </section>
  `,
})
export class ProfessionalTimelineComponent implements OnInit {
  private readonly api = inject(ProfessionalApiService);
  private readonly route = inject(ActivatedRoute);
  profileId = this.route.snapshot.paramMap.get('id') ?? '';
  timeline = signal<{ id: string; occurredAt: string; summary: string }[]>([]);

  ngOnInit(): void {
    if (!this.profileId) return;
    this.api.getTimeline(this.profileId).subscribe({ next: (t) => this.timeline.set(t) });
  }
}
