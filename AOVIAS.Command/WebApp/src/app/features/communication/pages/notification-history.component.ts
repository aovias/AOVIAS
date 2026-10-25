import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CommunicationApiService, TimelineItem } from '../services/communication-api.service';

@Component({
  selector: 'app-notification-history',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Notification History</h1><a routerLink="..">← Back</a></header>
      <ul>
        @for (e of events(); track e.id) {
          <li>{{ e.occurredAt | date: 'medium' }} — {{ e.eventType }}: {{ e.summary }}</li>
        }
      </ul>
    </section>
  `,
})
export class NotificationHistoryComponent implements OnInit {
  private api = inject(CommunicationApiService);
  events = signal<TimelineItem[]>([]);

  ngOnInit(): void {
    this.api.getHistory().subscribe({ next: (d) => this.events.set(d ?? []), error: () => {} });
  }
}
