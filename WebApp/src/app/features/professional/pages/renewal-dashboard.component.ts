import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProfessionalApiService, RenewalItem } from '../services/professional-api.service';

@Component({
  selector: 'app-renewal-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section style="padding:1.5rem">
      <a routerLink="/app/professional">← Directory</a>
      <h1>Renewal Dashboard</h1>
      <table>
        <thead><tr><th>Professional</th><th>Type</th><th>Item</th><th>Expires</th><th>Days</th></tr></thead>
        <tbody>
          @for (r of items(); track r.professionalProfileId + r.itemType + r.itemName) {
            <tr>
              <td><a [routerLink]="['/app/professional', r.professionalProfileId]">{{ r.personSearchName }}</a></td>
              <td>{{ r.itemType }}</td>
              <td>{{ r.itemName }}</td>
              <td>{{ r.expiryDate | date:'mediumDate' }}</td>
              <td>{{ r.daysUntilExpiry }}</td>
            </tr>
          }
        </tbody>
      </table>
    </section>
  `,
})
export class RenewalDashboardComponent implements OnInit {
  private readonly api = inject(ProfessionalApiService);
  items = signal<RenewalItem[]>([]);

  ngOnInit(): void {
    this.api.getRenewals(90).subscribe({ next: (items) => this.items.set(items) });
  }
}
