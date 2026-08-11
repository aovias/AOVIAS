import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { GovernanceApiService, TimelineItem } from '../services/governance-api.service';

@Component({
  selector: 'app-governance-timeline',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Governance Timeline</h1><a routerLink="/app/governance">Back</a></header>
      <ul>@for (e of events(); track e.id) {
        <li><strong>{{ e.eventType }}</strong> — {{ e.summary }} <em>{{ e.occurredAt | date:'medium' }}</em></li>
      }</ul>
    </section>
  `,
})
export class GovernanceTimelineComponent implements OnInit {
  private api = inject(GovernanceApiService);
  events = signal<TimelineItem[]>([]);

  ngOnInit(): void {
    this.api.getTimeline().subscribe((d) => this.events.set(d));
  }
}
