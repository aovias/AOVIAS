import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { GovernanceApiService } from '../services/governance-api.service';

@Component({
  selector: 'app-ownership-explorer',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section>
      <header><h1>Ownership Explorer</h1><a routerLink="/app/governance">Back</a></header>
      <form (ngSubmit)="search()">
        <input [(ngModel)]="resourceType" name="resourceType" placeholder="Resource type (e.g. Department)" />
        <input [(ngModel)]="resourceId" name="resourceId" placeholder="Resource ID" />
        <button type="submit">Search timeline</button>
      </form>
      <ul>
        @for (e of events(); track e.id) {
          <li>{{ e.occurredAt }} — {{ e.eventType }}: {{ e.summary }}</li>
        }
      </ul>
    </section>
  `,
})
export class OwnershipExplorerComponent {
  private api = inject(GovernanceApiService);
  resourceType = 'Department';
  resourceId = '';
  events = signal<{ id: string; eventType: string; summary: string; occurredAt: string }[]>([]);

  search(): void {
    this.api.getTimeline(this.resourceType, this.resourceId || undefined).subscribe((d) => this.events.set(d));
  }
}
