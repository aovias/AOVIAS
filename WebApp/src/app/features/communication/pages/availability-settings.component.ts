import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CommunicationApiService, AvailabilityProfile } from '../services/communication-api.service';

@Component({
  selector: 'app-availability-settings',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Availability Settings</h1><a routerLink="..">← Back</a></header>
      @if (profile()) {
        <p>Status: {{ profile()!.status }} · Timezone: {{ profile()!.timezone }}</p>
        <h3>Working hours</h3>
        <ul>
          @for (w of profile()!.workingHours; track w.dayOfWeek) {
            <li>Day {{ w.dayOfWeek }}: {{ w.startTime }} – {{ w.endTime }}</li>
          }
        </ul>
      } @else {
        <p>No availability profile configured.</p>
      }
    </section>
  `,
})
export class AvailabilitySettingsComponent implements OnInit {
  private api = inject(CommunicationApiService);
  profile = signal<AvailabilityProfile | null>(null);

  ngOnInit(): void {
    this.api.getAvailability().subscribe({ next: (d) => this.profile.set(d), error: () => {} });
  }
}
