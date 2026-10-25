import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SecurityApiService, SecurityTimelineItem } from '../services/security-api.service';

@Component({
  selector: 'app-security-timeline',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page">
      <h1><a routerLink="..">Security</a> / Timeline</h1>
      @for (e of events(); track e.id) {
        <div class="row">
          <time>{{ e.occurredAt | date:'medium' }}</time>
          <span class="type">{{ e.source }} · {{ e.eventType }}</span>
          <span>{{ e.summary }}</span>
        </div>
      }
    </section>
  `,
  styles: [`.page { padding: 1.5rem; } .row { padding: 0.5rem 0; border-bottom: 1px solid #eee; } .type { color: #666; margin: 0 0.5rem; }`],
})
export class SecurityTimelineComponent implements OnInit {
  private api = inject(SecurityApiService);
  events = signal<SecurityTimelineItem[]>([]);

  ngOnInit(): void {
    this.api.getTimeline().subscribe({ next: (items) => this.events.set(items), error: () => {} });
  }
}
