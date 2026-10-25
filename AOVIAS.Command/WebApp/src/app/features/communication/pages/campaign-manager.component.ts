import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CommunicationApiService, CampaignItem } from '../services/communication-api.service';

@Component({
  selector: 'app-campaign-manager',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Campaign Manager</h1><a routerLink="..">← Back</a></header>
      <ul>
        @for (c of campaigns(); track c.id) {
          <li>{{ c.name }} · {{ c.moduleCode }} · {{ c.status }}</li>
        }
      </ul>
    </section>
  `,
})
export class CampaignManagerComponent implements OnInit {
  private api = inject(CommunicationApiService);
  campaigns = signal<CampaignItem[]>([]);

  ngOnInit(): void {
    this.api.listCampaigns().subscribe({ next: (d) => this.campaigns.set(d ?? []), error: () => {} });
  }
}
