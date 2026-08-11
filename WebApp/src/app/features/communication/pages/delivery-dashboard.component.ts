import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CommunicationApiService, DeliveryDashboard } from '../services/communication-api.service';

@Component({
  selector: 'app-delivery-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Delivery Dashboard</h1><a routerLink="..">← Back</a></header>
      @if (stats()) {
        <ul class="stats">
          <li>Pending outbound: {{ stats()!.pendingOutbound }}</li>
          <li>Failed deliveries: {{ stats()!.failedDeliveries }}</li>
          <li>Unread in-app: {{ stats()!.unreadInApp }}</li>
        </ul>
      }
    </section>
  `,
})
export class DeliveryDashboardComponent implements OnInit {
  private api = inject(CommunicationApiService);
  stats = signal<DeliveryDashboard | null>(null);

  ngOnInit(): void {
    this.api.getDashboard().subscribe({ next: (d) => this.stats.set(d), error: () => {} });
  }
}
